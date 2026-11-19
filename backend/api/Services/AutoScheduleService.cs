using System.Globalization;
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
        Task<IList<AutoScheduleOccurrenceResponse>> ReadUpcoming(string installationCode);
        Task<bool> SkipOccurrence(MissionDefinition missionDefinition, DateTimeOffset scheduledAt);
        Task<bool> UnskipOccurrence(MissionDefinition missionDefinition, long skipId);
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
                    s.MissionDefinition.Id == definition.Id && s.ScheduledAt > now
                )
                .Select(s => s.ScheduledAt)
                .ToListAsync();
            foreach (var instant in instants.Where(i => !skips.Contains(i)))
                RegisterJob(definition.Id, instant);
        }

        private void RegisterJob(string definitionId, DateTimeOffset instant)
        {
            // This cache avoids redundant registrations, but concurrent jobs can still race.
            if (jobs.Contains(definitionId, instant))
                return;
            backgroundJobs.Schedule(() => DispatchOccurrence(definitionId, instant), instant);
            jobs.Register(definitionId, instant);
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
                    ids.Contains(s.MissionDefinition.Id)
                    && s.ScheduledAt > now
                    && s.ScheduledAt <= until
                )
                .Select(s => new
                {
                    s.Id,
                    MissionDefinitionId = s.MissionDefinition.Id,
                    s.ScheduledAt,
                })
                .ToListAsync();
            var skipsByInstant = skips.ToDictionary(s => (s.MissionDefinitionId, s.ScheduledAt));
            return definitions
                .SelectMany(d =>
                    (d.AutoScheduleFrequency?.GetSchedulingInstants(now, until) ?? []).Select(
                        instant =>
                        {
                            skipsByInstant.TryGetValue((d.Id, instant), out var skip);
                            return new AutoScheduleOccurrenceResponse(
                                instant,
                                skip != null
                                    ? AutoScheduleOccurrenceStatus.Skipped
                                    : AutoScheduleOccurrenceStatus.Scheduled,
                                new MissionDefinitionResponse(d),
                                skip?.Id.ToString(CultureInfo.InvariantCulture)
                            );
                        }
                    )
                )
                .OrderBy(o => o.ScheduledAt)
                .ThenBy(o => o.Mission.Id)
                .ToList();
        }

        public Task<bool> SkipOccurrence(
            MissionDefinition missionDefinition,
            DateTimeOffset scheduledAt
        ) => ChangeDatedSkip(missionDefinition, scheduledAt, null);

        public Task<bool> UnskipOccurrence(MissionDefinition missionDefinition, long skipId) =>
            ChangeDatedSkip(missionDefinition, null, skipId);

        private async Task<bool> ChangeDatedSkip(
            MissionDefinition missionDefinition,
            DateTimeOffset? scheduledAt,
            long? skipId
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
                        readOnly: false,
                        accessMode: AccessMode.Write
                    );
                    var instant = scheduledAt.HasValue
                        ? TimeZoneUtilities.ToPostgresPrecision(scheduledAt.Value)
                        : await context
                            .AutoScheduleSkips.Where(s =>
                                s.Id == skipId && s.MissionDefinition.Id == missionDefinition.Id
                            )
                            .Select(s => (DateTimeOffset?)s.ScheduledAt)
                            .SingleOrDefaultAsync();
                    if (
                        current == null
                        || !instant.HasValue
                        || !CanChangeOccurrenceSkip(current, instant.Value)
                    )
                    {
                        logger.LogWarning(
                            "Cannot change occurrence skip: it is no longer eligible"
                        );
                        return null;
                    }
                    if (!skipId.HasValue)
                        await AddDatedSkipIfMissing(current, instant.Value);
                    else if (
                        !await RemoveDatedSkipAndRegisterJob(
                            missionDefinition.Id,
                            skipId.Value,
                            instant.Value
                        )
                    )
                        return null;
                    await transaction.CommitAsync();
                    return current;
                });
            if (affectedDefinition != null)
            {
                Notify(affectedDefinition);
                signalRService.ReportAutoScheduleToSignalR(
                    skipId.HasValue ? "unskipAutoMission" : "skipAutoMission",
                    affectedDefinition.Name,
                    skipId.HasValue
                        ? "Restored the selected dated mission."
                        : "Skipped the selected dated mission.",
                    affectedDefinition.InstallationCode
                );
            }
            return affectedDefinition != null;
        }

        private bool CanChangeOccurrenceSkip(
            MissionDefinition definition,
            DateTimeOffset scheduledAt
        )
        {
            var now = timeProvider.GetUtcNow();
            return definition
                    .AutoScheduleFrequency?.GetSchedulingInstants(now, now.AddHours(24))
                    .Contains(scheduledAt) == true;
        }

        private async Task AddDatedSkipIfMissing(
            MissionDefinition missionDefinition,
            DateTimeOffset scheduledAt
        )
        {
            var skip = new AutoScheduleSkip
            {
                MissionDefinition = missionDefinition,
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

        private async Task<bool> RemoveDatedSkipAndRegisterJob(
            string missionDefinitionId,
            long skipId,
            DateTimeOffset scheduledAt
        )
        {
            var removed = await context
                .AutoScheduleSkips.Where(s =>
                    s.MissionDefinition.Id == missionDefinitionId && s.Id == skipId
                )
                .ExecuteDeleteAsync();
            if (removed == 0)
            {
                logger.LogInformation("The selected skip has already been removed");
                return false;
            }
            // A restart may have omitted this job because the skip was persisted.
            // Register before commit so a registration failure leaves the skip intact.
            RegisterJob(missionDefinitionId, scheduledAt);
            return true;
        }

        public async Task DispatchOccurrence(string missionDefinitionId, DateTimeOffset scheduledAt)
        {
            scheduledAt = TimeZoneUtilities.ToPostgresPrecision(scheduledAt);
            var robot = await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    return await CreateDueMissionRunIfNotQueued(missionDefinitionId, scheduledAt);
                });
            if (robot != null)
                await missionSchedulingService.StartNextMissionRunIfSystemIsAvailable(robot);
        }

        private async Task<Robot?> CreateDueMissionRunIfNotQueued(
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
            if (
                await context.AutoScheduleSkips.AnyAsync(s =>
                    s.MissionDefinition.Id == missionDefinitionId && s.ScheduledAt == scheduledAt
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
                return robot;
            }
            await missionRunService.Create(CreateScheduledMissionRun(definition, robot, now));
            return robot;
        }

        private static MissionRun CreateScheduledMissionRun(
            MissionDefinition definition,
            Robot robot,
            DateTimeOffset now
        )
        {
            var run = new MissionRun
            {
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
