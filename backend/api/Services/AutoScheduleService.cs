using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Utilities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services
{
    public interface IAutoScheduleService
    {
        Task Reconcile(MissionDefinition missionDefinition);
        Task<MissionDefinition> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            IList<TimeAndDay>? newAutoScheduleFrequency,
            UpdateMissionDefinitionQuery? requestedChanges = null
        );
        Task<IList<AutoScheduleOccurrenceResponse>> ReadUpcoming(string installationCode);
        Task<bool> SkipOccurrence(MissionDefinition missionDefinition, string scheduleKey);
        Task<bool> UnskipOccurrence(MissionDefinition missionDefinition, string scheduleKey);
    }

    public class AutoScheduleService(
        ILogger<AutoScheduleService> logger,
        IMissionDefinitionService missionDefinitionService,
        IRobotService robotService,
        IMissionRunService missionRunService,
        IMissionSchedulingService missionSchedulingService,
        ISignalRService signalRService,
        IAccessRoleService accessRoleService,
        IBackgroundJobClient backgroundJobs,
        FlotillaDbContext context,
        TimeProvider timeProvider,
        AutoScheduleJobs jobs
    ) : IAutoScheduleService
    {
        private async Task EnsureWriteAccess(MissionDefinition definition)
        {
            var allowed = await accessRoleService.GetAllowedInstallationCodes(AccessMode.Write);
            if (!allowed.Contains(definition.InstallationCode, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Denied auto scheduling change: installation write access required"
                );
                throw new UnauthorizedAccessException(
                    "Installation write access is required to change auto scheduling."
                );
            }
        }

        public async Task Reconcile(MissionDefinition missionDefinition)
        {
            await EnsureWriteAccess(missionDefinition);
            var current = await missionDefinitionService.ReadById(
                missionDefinition.Id,
                accessMode: AccessMode.Write
            );
            if (current == null)
            {
                logger.LogInformation("Ignoring schedule for an unavailable mission definition");
                return;
            }
            await RegisterJobs(current);
        }

        private async Task RegisterJobs(MissionDefinition definition)
        {
            var now = timeProvider.GetUtcNow();
            jobs.Prune(now);
            var instants =
                definition.AutoScheduleFrequency?.GetSchedulingInstants(now, now.AddHours(48))
                ?? [];
            var skips = await context
                .AutoScheduleSkips.Where(s =>
                    s.MissionDefinitionId == definition.Id && s.ScheduledAt > now
                )
                .Select(s => s.ScheduledAt)
                .ToListAsync();
            foreach (var instant in instants.Where(i => !skips.Contains(i)))
                RegisterJob(definition.Id, instant);
        }

        private void RegisterJob(string definitionId, DateTimeOffset instant)
        {
            // Concurrent registration can enqueue duplicates; the run primary key prevents
            // duplicate runs. This cache only avoids redundant registrations in normal use.
            var scheduleKey = AutoScheduleKey.Create(definitionId, instant);
            if (jobs.Contains(scheduleKey))
                return;
            backgroundJobs.Schedule(() => DispatchOccurrence(definitionId, instant), instant);
            jobs.Register(scheduleKey, instant);
        }

        public async Task<MissionDefinition> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            IList<TimeAndDay>? newAutoScheduleFrequency,
            UpdateMissionDefinitionQuery? requestedChanges = null
        )
        {
            await EnsureWriteAccess(missionDefinition);
            context.ChangeTracker.Clear();
            // Track the current owned tasks/schedule so replacements and removals are saved.
            var current =
                await missionDefinitionService.ReadById(
                    missionDefinition.Id,
                    readOnly: false,
                    accessMode: AccessMode.Write
                )
                ?? throw new InvalidOperationException(
                    "Mission definition is no longer available for changes."
                );
            if (requestedChanges?.Name != null)
                current.Name = requestedChanges.Value.Name;
            if (requestedChanges?.Comment != null)
                current.Comment = requestedChanges.Value.Comment;
            if (requestedChanges?.Tasks != null)
            {
                current.Tasks = missionDefinition.Tasks;
                if (current.InspectionArea.Id != missionDefinition.InspectionArea.Id)
                    current.InspectionArea = missionDefinition.InspectionArea;
            }
            // Null on PATCH means omitted, while an empty list explicitly removes the schedule.
            if (
                requestedChanges == null
                || requestedChanges.Value.SchedulingTimesCETperWeek != null
            )
                current.AutoScheduleFrequency =
                    newAutoScheduleFrequency?.Count > 0
                        ? new AutoScheduleFrequency
                        {
                            SchedulingTimesCETperWeek = newAutoScheduleFrequency,
                        }
                        : null;
            var updated = await missionDefinitionService.Update(current);
            if (
                requestedChanges == null
                || requestedChanges.Value.SchedulingTimesCETperWeek != null
            )
                await Reconcile(updated);
            return updated;
        }

        public async Task<IList<AutoScheduleOccurrenceResponse>> ReadUpcoming(
            string installationCode
        )
        {
            var definitions = (
                await missionDefinitionService.ReadByInstallationCode(installationCode)
            ).ToList();
            var ids = definitions.Select(d => d.Id).ToList();
            var now = timeProvider.GetUtcNow();
            var until = now.AddHours(24);
            var skips = await context
                .AutoScheduleSkips.Where(s =>
                    ids.Contains(s.MissionDefinitionId)
                    && s.ScheduledAt > now
                    && s.ScheduledAt <= until
                )
                .ToListAsync();
            var skippedInstants = skips
                .Select(s => (s.MissionDefinitionId, s.ScheduledAt))
                .ToHashSet();
            return definitions
                .SelectMany(d =>
                    (d.AutoScheduleFrequency?.GetSchedulingInstants(now, until) ?? []).Select(
                        instant => new AutoScheduleOccurrenceResponse(
                            AutoScheduleKey.Create(d.Id, instant),
                            instant,
                            skippedInstants.Contains((d.Id, instant))
                                ? AutoScheduleOccurrenceStatus.Skipped
                                : AutoScheduleOccurrenceStatus.Scheduled,
                            new MissionDefinitionResponse(d)
                        )
                    )
                )
                .OrderBy(o => o.ScheduledAt)
                .ThenBy(o => o.ScheduleKey)
                .ToList();
        }

        public Task<bool> SkipOccurrence(MissionDefinition missionDefinition, string scheduleKey) =>
            SetOccurrenceSkipped(missionDefinition, scheduleKey, true);

        public Task<bool> UnskipOccurrence(
            MissionDefinition missionDefinition,
            string scheduleKey
        ) => SetOccurrenceSkipped(missionDefinition, scheduleKey, false);

        private async Task<bool> SetOccurrenceSkipped(
            MissionDefinition missionDefinition,
            string scheduleKey,
            bool skip
        )
        {
            await EnsureWriteAccess(missionDefinition);
            var affectedDefinition = await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    var current = await missionDefinitionService.ReadById(
                        missionDefinition.Id,
                        accessMode: AccessMode.Write
                    );
                    var scheduledAt = await FindEligibleSkipTime(current, scheduleKey);
                    if (scheduledAt == null)
                    {
                        logger.LogWarning(
                            "Cannot change occurrence skip: it is no longer eligible"
                        );
                        return null;
                    }
                    if (skip)
                        await AddDatedSkipIfMissing(missionDefinition.Id, scheduledAt.Value);
                    else
                        await RemoveDatedSkipAndRegisterJob(
                            missionDefinition.Id,
                            scheduledAt.Value
                        );
                    await transaction.CommitAsync();
                    return current;
                });
            if (affectedDefinition != null)
            {
                Notify(affectedDefinition);
                signalRService.ReportAutoScheduleToSignalR(
                    skip ? "skipAutoMission" : "unskipAutoMission",
                    affectedDefinition.Name,
                    skip
                        ? "Skipped the selected dated mission."
                        : "Restored the selected dated mission.",
                    affectedDefinition.InstallationCode
                );
            }
            return affectedDefinition != null;
        }

        private async Task<DateTimeOffset?> FindEligibleSkipTime(
            MissionDefinition? definition,
            string scheduleKey
        )
        {
            if (definition == null)
                return null;
            var now = timeProvider.GetUtcNow();
            var scheduledAt = definition
                .AutoScheduleFrequency?.GetSchedulingInstants(now, now.AddHours(24))
                .Where(i => AutoScheduleKey.Create(definition.Id, i) == scheduleKey)
                .Select(i => (DateTimeOffset?)i)
                .SingleOrDefault();
            if (scheduledAt == null || await context.MissionRuns.AnyAsync(r => r.Id == scheduleKey))
                return null;
            return scheduledAt;
        }

        private async Task AddDatedSkipIfMissing(
            string missionDefinitionId,
            DateTimeOffset scheduledAt
        )
        {
            var skip = new AutoScheduleSkip
            {
                MissionDefinitionId = missionDefinitionId,
                ScheduledAt = scheduledAt,
            };
            context.AutoScheduleSkips.Add(skip);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException e)
                when (e.InnerException
                        is PostgresException
                        {
                            SqlState: PostgresErrorCodes.UniqueViolation,
                            ConstraintName: "IX_AutoScheduleSkips_MissionDefinitionId_ScheduledAt"
                        }
                )
            {
                // EF rolls back to its savepoint; detach the rejected insert before continuing.
                context.Entry(skip).State = EntityState.Detached;
                logger.LogInformation("The selected dated mission is already skipped");
            }
        }

        private async Task RemoveDatedSkipAndRegisterJob(
            string missionDefinitionId,
            DateTimeOffset scheduledAt
        )
        {
            await context
                .AutoScheduleSkips.Where(s =>
                    s.MissionDefinitionId == missionDefinitionId && s.ScheduledAt == scheduledAt
                )
                .ExecuteDeleteAsync();
            // A restart may have omitted this job because the skip was persisted.
            // Register before commit so a registration failure leaves the skip intact.
            RegisterJob(missionDefinitionId, scheduledAt);
        }

        public async Task DispatchOccurrence(string missionDefinitionId, DateTimeOffset scheduledAt)
        {
            scheduledAt = TimeZoneUtilities.ToPostgresPrecision(scheduledAt);
            var run = await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    var candidate = await PrepareDueMissionRun(missionDefinitionId, scheduledAt);
                    if (candidate == null)
                        return null;
                    try
                    {
                        return await missionRunService.Create(candidate);
                    }
                    catch (DbUpdateException e)
                        when (e.InnerException
                                is PostgresException
                                {
                                    SqlState: PostgresErrorCodes.UniqueViolation,
                                    ConstraintName: "PK_MissionRuns"
                                }
                        )
                    {
                        context.ChangeTracker.Clear();
                        logger.LogInformation("Ignoring duplicate auto schedule dispatch");
                        return null;
                    }
                });
            if (run != null)
                await missionSchedulingService.StartNextMissionRunIfSystemIsAvailable(run.Robot);
        }

        private async Task<MissionRun?> PrepareDueMissionRun(
            string missionDefinitionId,
            DateTimeOffset scheduledAt
        )
        {
            var definition = await missionDefinitionService.ReadById(
                missionDefinitionId,
                accessMode: AccessMode.Write
            );
            var now = timeProvider.GetUtcNow();
            // No startup catch-up or early execution; delayed jobs from this process remain eligible.
            if (
                definition == null
                || scheduledAt <= jobs.StartedAt
                || scheduledAt > now
                || definition
                    .AutoScheduleFrequency?.GetSchedulingInstants(
                        scheduledAt.AddTicks(-1),
                        scheduledAt
                    )
                    .Contains(scheduledAt) != true
            )
            {
                logger.LogInformation(
                    "Ignoring unavailable, stale or not-yet-due auto schedule job"
                );
                return null;
            }
            var scheduleKey = AutoScheduleKey.Create(missionDefinitionId, scheduledAt);
            if (
                await context.MissionRuns.AnyAsync(r => r.Id == scheduleKey)
                || await context.AutoScheduleSkips.AnyAsync(s =>
                    s.MissionDefinitionId == missionDefinitionId && s.ScheduledAt == scheduledAt
                )
            )
                return null;

            var robots = await robotService.ReadRobotsForInstallation(definition.InstallationCode);
            var robot = robots.FirstOrDefault(r =>
                r.CurrentInspectionAreaId == definition.InspectionArea.Id
            );
            if (robot == null)
            {
                logger.LogWarning("No robot available for due auto scheduled mission");
                signalRService.ReportAutoScheduleToSignalR(
                    "AutoScheduleFail",
                    definition.Name,
                    "No robot available for the auto scheduled mission.",
                    definition.InstallationCode
                );
                return null;
            }
            var queued = await missionRunService.ReadMissionRunQueue(robot.Id);
            if (queued.Any(r => r.MissionId == definition.Id))
            {
                logger.LogInformation("Auto scheduled mission is already queued");
                return null;
            }
            return CreateScheduledMissionRun(definition, robot, scheduleKey, now);
        }

        private static MissionRun CreateScheduledMissionRun(
            MissionDefinition definition,
            Robot robot,
            string scheduleKey,
            DateTimeOffset now
        )
        {
            var run = new MissionRun
            {
                Id = scheduleKey,
                MissionId = definition.Id,
                Name = definition.Name,
                Robot = robot,
                Status = MissionStatus.Queued,
                CreationTime = now.UtcDateTime,
                Tasks = [.. definition.Tasks.Select(t => t.ToMissionRunTask())],
                InstallationCode = definition.InstallationCode,
                InspectionArea = definition.InspectionArea,
            };
            if (run.Tasks.Any())
                run.SetEstimatedTaskDuration();
            return run;
        }

        private void Notify(MissionDefinition definition) =>
            _ = signalRService.SendMessageAsync(
                "Mission definition updated",
                definition.InspectionArea.Installation,
                new MissionDefinitionResponse(definition)
            );
    }
}
