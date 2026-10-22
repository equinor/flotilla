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
        Task<bool> SkipOccurrence(MissionDefinition missionDefinition, string occurrenceId);
        Task<bool> UnskipOccurrence(MissionDefinition missionDefinition, string occurrenceId);
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
            var id = AutoScheduleSkip.OccurrenceId(definitionId, instant);
            if (jobs.Contains(id))
                return;
            backgroundJobs.Schedule(() => DispatchOccurrence(definitionId, instant), instant);
            jobs.Register(id, instant);
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
                            AutoScheduleSkip.OccurrenceId(d.Id, instant),
                            instant,
                            skippedInstants.Contains((d.Id, instant))
                                ? AutoScheduleOccurrenceStatus.Skipped
                                : AutoScheduleOccurrenceStatus.Scheduled,
                            new MissionDefinitionResponse(d)
                        )
                    )
                )
                .OrderBy(o => o.ScheduledAt)
                .ThenBy(o => o.Id)
                .ToList();
        }

        public Task<bool> SkipOccurrence(
            MissionDefinition missionDefinition,
            string occurrenceId
        ) => SetOccurrenceSkipped(missionDefinition, occurrenceId, true);

        public Task<bool> UnskipOccurrence(
            MissionDefinition missionDefinition,
            string occurrenceId
        ) => SetOccurrenceSkipped(missionDefinition, occurrenceId, false);

        private async Task<bool> SetOccurrenceSkipped(
            MissionDefinition missionDefinition,
            string occurrenceId,
            bool skip
        )
        {
            await EnsureWriteAccess(missionDefinition);
            var updatedDefinition = await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    var current = await missionDefinitionService.ReadById(
                        missionDefinition.Id,
                        accessMode: AccessMode.Write
                    );
                    var now = timeProvider.GetUtcNow();
                    var instant = current
                        ?.AutoScheduleFrequency?.GetSchedulingInstants(now, now.AddHours(24))
                        .Where(i =>
                            AutoScheduleSkip.OccurrenceId(missionDefinition.Id, i) == occurrenceId
                        )
                        .Select(i => (DateTimeOffset?)i)
                        .SingleOrDefault();
                    if (
                        instant == null
                        || await context.MissionRuns.AnyAsync(r => r.Id == occurrenceId)
                    )
                    {
                        logger.LogWarning(
                            "Cannot change occurrence skip: it is no longer eligible"
                        );
                        return null;
                    }
                    var skips = context.AutoScheduleSkips.Where(s =>
                        s.MissionDefinitionId == missionDefinition.Id
                        && s.ScheduledAt == instant.Value
                    );
                    if (skip)
                    {
                        await context.Database.ExecuteSqlInterpolatedAsync(
                            $"""
                            INSERT INTO "AutoScheduleSkips" ("MissionDefinitionId", "ScheduledAt")
                            VALUES ({missionDefinition.Id}, {instant.Value})
                            ON CONFLICT ("MissionDefinitionId", "ScheduledAt") DO NOTHING
                            """
                        );
                    }
                    else
                    {
                        await skips.ExecuteDeleteAsync();
                        // A restart may have omitted this job because the skip was persisted.
                        // Register before commit so a registration failure leaves the skip intact.
                        RegisterJob(missionDefinition.Id, instant.Value);
                    }
                    await transaction.CommitAsync();
                    return current;
                });
            if (updatedDefinition != null)
            {
                Notify(updatedDefinition);
                signalRService.ReportAutoScheduleToSignalR(
                    skip ? "skipAutoMission" : "unskipAutoMission",
                    updatedDefinition.Name,
                    skip
                        ? "Skipped the selected dated mission."
                        : "Restored the selected dated mission.",
                    updatedDefinition.InstallationCode
                );
            }
            return updatedDefinition != null;
        }

        public async Task DispatchOccurrence(string missionDefinitionId, DateTimeOffset scheduledAt)
        {
            scheduledAt = TimeZoneUtilities.ToPostgresPrecision(scheduledAt);
            var run = await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    var definition = await missionDefinitionService.ReadById(
                        missionDefinitionId,
                        accessMode: AccessMode.Write
                    );
                    var now = timeProvider.GetUtcNow();
                    // Jobs lost on restart are not caught up. Delayed jobs from this process may run,
                    // but no job may create a mission before its exact scheduled instant.
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
                    var id = AutoScheduleSkip.OccurrenceId(missionDefinitionId, scheduledAt);
                    if (
                        await context.MissionRuns.AnyAsync(r => r.Id == id)
                        || await context.AutoScheduleSkips.AnyAsync(s =>
                            s.MissionDefinitionId == missionDefinitionId
                            && s.ScheduledAt == scheduledAt
                        )
                    )
                        return null;
                    var robots = await robotService.ReadRobotsForInstallation(
                        definition.InstallationCode
                    );
                    var robot = robots.FirstOrDefault(r =>
                        r.CurrentInspectionAreaId == definition.InspectionArea.Id
                    );
                    if (robot == null)
                    {
                        logger.LogWarning("No robot available for due auto scheduled mission");
                        await transaction.CommitAsync();
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
                    var created = new MissionRun
                    {
                        Id = id,
                        MissionId = definition.Id,
                        Name = definition.Name,
                        Robot = robot,
                        Status = MissionStatus.Queued,
                        CreationTime = now.UtcDateTime,
                        Tasks = [.. definition.Tasks.Select(t => t.ToMissionRunTask())],
                        InstallationCode = definition.InstallationCode,
                        InspectionArea = definition.InspectionArea,
                    };
                    if (created.Tasks.Any())
                        created.SetEstimatedTaskDuration();
                    try
                    {
                        await missionRunService.Create(created);
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
                        await transaction.RollbackAsync();
                        context.ChangeTracker.Clear();
                        logger.LogInformation("Ignoring duplicate auto schedule dispatch");
                        return null;
                    }
                    await transaction.CommitAsync();
                    return created;
                });
            if (run != null)
            {
                _ = signalRService.SendMessageAsync(
                    "Mission run created",
                    run.InspectionArea.Installation,
                    new MissionRunResponse(run)
                );
                await missionSchedulingService.StartNextMissionRunIfSystemIsAvailable(run.Robot);
            }
        }

        private void Notify(MissionDefinition definition) =>
            _ = signalRService.SendMessageAsync(
                "Mission definition updated",
                definition.InspectionArea.Installation,
                new MissionDefinitionResponse(definition)
            );
    }
}
