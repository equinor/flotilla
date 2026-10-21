using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

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
        TimeProvider timeProvider
    ) : IAutoScheduleService
    {
        private static readonly string SchedulerInstance = Guid.NewGuid().ToString();
        private static readonly TimeSpan ReconciliationHorizon = TimeSpan.FromHours(48);

        private async Task EnsureWriteAccess(MissionDefinition missionDefinition)
        {
            var allowedInstallationCodes = await accessRoleService.GetAllowedInstallationCodes(
                AccessMode.Write
            );
            if (
                !allowedInstallationCodes.Contains(
                    missionDefinition.InstallationCode,
                    StringComparer.OrdinalIgnoreCase
                )
            )
            {
                logger.LogWarning(
                    "Denied auto scheduling change for mission definition {Id}",
                    missionDefinition.Id
                );
                throw new UnauthorizedAccessException(
                    "Installation write access is required to change auto scheduling."
                );
            }
        }

        public async Task Reconcile(MissionDefinition missionDefinition)
        {
            await EnsureWriteAccess(missionDefinition);
            await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    // Keep the definition unchanged while deriving and scheduling its occurrences.
                    var current = await missionDefinitionService.ReadById(
                        missionDefinition.Id,
                        forUpdate: true
                    );
                    if (current == null)
                    {
                        logger.LogInformation(
                            "Skipping auto schedule reconciliation: mission definition is no longer available"
                        );
                        return;
                    }
                    var occurrencesToCancel = await ReconcileLocked(current);
                    await transaction.CommitAsync();
                    occurrencesToCancel.ForEach(CancelJob);
                    Notify(current);
                });
        }

        private async Task<List<AutoScheduleOccurrence>> ReconcileLocked(
            MissionDefinition missionDefinition
        )
        {
            var now = timeProvider.GetUtcNow();
            var desiredSchedulingInstants =
                missionDefinition
                    .AutoScheduleFrequency?.GetSchedulingInstants(
                        now,
                        now.Add(ReconciliationHorizon)
                    )
                    .ToHashSet()
                ?? [];
            var currentAutoScheduleOccurrences = await context
                .AutoScheduleOccurrences.Where(o =>
                    o.MissionDefinitionId == missionDefinition.Id
                    && (o.ScheduledAt > now || o.Status == AutoScheduleOccurrenceStatus.Scheduled)
                )
                .ToListAsync();

            var occurrencesToCancel = await MarkObsoleteOccurrences(
                currentAutoScheduleOccurrences,
                desiredSchedulingInstants,
                now
            );
            foreach (var instant in desiredSchedulingInstants.Order())
            {
                var scheduledOccurrence = currentAutoScheduleOccurrences.SingleOrDefault(o =>
                    o.ScheduledAt == instant
                );
                await EnsureOccurrenceScheduled(missionDefinition.Id, instant, scheduledOccurrence);
            }
            return occurrencesToCancel;
        }

        private async Task<List<AutoScheduleOccurrence>> MarkObsoleteOccurrences(
            IEnumerable<AutoScheduleOccurrence> currentAutoScheduleOccurrences,
            HashSet<DateTimeOffset> desiredSchedulingInstants,
            DateTimeOffset now
        )
        {
            var occurrencesToCancel = new List<AutoScheduleOccurrence>();
            foreach (
                var occurrence in currentAutoScheduleOccurrences.Where(o =>
                    o.Status == AutoScheduleOccurrenceStatus.Scheduled
                )
            )
            {
                if (
                    occurrence.ScheduledAt <= now
                    && occurrence.SchedulerInstance == SchedulerInstance
                )
                    continue;
                if (
                    occurrence.ScheduledAt <= now
                    || !desiredSchedulingInstants.Contains(occurrence.ScheduledAt)
                )
                {
                    var status =
                        occurrence.ScheduledAt <= now
                            ? AutoScheduleOccurrenceStatus.Missed
                            : AutoScheduleOccurrenceStatus.Cancelled;
                    await context
                        .AutoScheduleOccurrences.Where(o =>
                            o.Id == occurrence.Id
                            && o.Status == AutoScheduleOccurrenceStatus.Scheduled
                        )
                        .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, status));
                    occurrencesToCancel.Add(occurrence);
                    occurrence.Status = status;
                }
            }
            return occurrencesToCancel;
        }

        private async Task EnsureOccurrenceScheduled(
            string missionDefinitionId,
            DateTimeOffset instant,
            AutoScheduleOccurrence? scheduledOccurrence
        )
        {
            if (scheduledOccurrence == null)
            {
                scheduledOccurrence = new AutoScheduleOccurrence
                {
                    MissionDefinitionId = missionDefinitionId,
                    ScheduledAt = instant,
                    // Pending dispatch; the mission run is created only when the job fires.
                    Status = AutoScheduleOccurrenceStatus.Scheduled,
                };
                context.AutoScheduleOccurrences.Add(scheduledOccurrence);
                await context.SaveChangesAsync();
                context.Entry(scheduledOccurrence).State = EntityState.Detached;
            }
            else if (scheduledOccurrence.Status == AutoScheduleOccurrenceStatus.Cancelled)
            {
                // Re-adding a removed weekly slot restores it, but never restores a skip.
                await context
                    .AutoScheduleOccurrences.Where(o => o.Id == scheduledOccurrence.Id)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(o => o.Status, AutoScheduleOccurrenceStatus.Scheduled)
                            .SetProperty(o => o.SchedulerInstance, (string?)null)
                    );
                scheduledOccurrence.Status = AutoScheduleOccurrenceStatus.Scheduled;
                scheduledOccurrence.SchedulerInstance = null;
            }

            if (
                scheduledOccurrence.Status != AutoScheduleOccurrenceStatus.Scheduled
                || scheduledOccurrence.SchedulerInstance == SchedulerInstance
            )
                return;

            await ScheduleDispatchJob(scheduledOccurrence);
        }

        private async Task ScheduleDispatchJob(AutoScheduleOccurrence scheduledOccurrence)
        {
            var jobId = backgroundJobs.Schedule(
                () => DispatchOccurrence(scheduledOccurrence.Id),
                scheduledOccurrence.ScheduledAt
            );
            try
            {
                await context
                    .AutoScheduleOccurrences.Where(o =>
                        o.Id == scheduledOccurrence.Id
                        && o.Status == AutoScheduleOccurrenceStatus.Scheduled
                    )
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(o => o.JobId, jobId)
                            .SetProperty(o => o.SchedulerInstance, SchedulerInstance)
                    );
            }
            catch
            {
                CancelJob(
                    new AutoScheduleOccurrence
                    {
                        MissionDefinitionId = scheduledOccurrence.MissionDefinitionId,
                        JobId = jobId,
                        SchedulerInstance = SchedulerInstance,
                    }
                );
                throw;
            }
        }

        public async Task<MissionDefinition> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            IList<TimeAndDay>? newAutoScheduleFrequency,
            UpdateMissionDefinitionQuery? requestedChanges = null
        )
        {
            await EnsureWriteAccess(missionDefinition);
            if (newAutoScheduleFrequency?.Count == 0)
                newAutoScheduleFrequency = null;

            return await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    // Discard the caller's tracked snapshot; explicit PATCH edits are applied below.
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    // Hold the row lock through the definition update and occurrence scheduling.
                    var lockedDefinition = await missionDefinitionService.ReadById(
                        missionDefinition.Id,
                        accessMode: AccessMode.Write,
                        forUpdate: true
                    );
                    if (lockedDefinition == null)
                        throw new InvalidOperationException(
                            "Mission definition is no longer available for schedule changes."
                        );
                    if (requestedChanges?.Name != null)
                        lockedDefinition.Name = requestedChanges.Value.Name;
                    if (requestedChanges?.Comment != null)
                        lockedDefinition.Comment = requestedChanges.Value.Comment;
                    if (requestedChanges?.Tasks != null)
                    {
                        // The controller has validated these tasks and selected their inspection area.
                        lockedDefinition.Tasks = missionDefinition.Tasks;
                        lockedDefinition.InspectionArea = missionDefinition.InspectionArea;
                    }
                    lockedDefinition.AutoScheduleFrequency =
                        newAutoScheduleFrequency == null
                            ? null
                            : new AutoScheduleFrequency
                            {
                                SchedulingTimesCETperWeek = newAutoScheduleFrequency,
                            };
                    await missionDefinitionService.Update(lockedDefinition);
                    var occurrencesToCancel = await ReconcileLocked(lockedDefinition);
                    await transaction.CommitAsync();
                    occurrencesToCancel.ForEach(CancelJob);
                    Notify(lockedDefinition);
                    return lockedDefinition;
                });
        }

        public async Task<IList<AutoScheduleOccurrenceResponse>> ReadUpcoming(
            string installationCode
        )
        {
            var definitions = (
                await missionDefinitionService.ReadByInstallationCode(installationCode)
            ).ToDictionary(m => m.Id);
            var ids = definitions.Keys.ToList();
            var now = timeProvider.GetUtcNow();
            var until = now.AddHours(24);
            var occurrences = await context
                .AutoScheduleOccurrences.Where(o =>
                    ids.Contains(o.MissionDefinitionId)
                    && o.ScheduledAt > now
                    && o.ScheduledAt <= until
                    && (
                        o.Status == AutoScheduleOccurrenceStatus.Scheduled
                        || o.Status == AutoScheduleOccurrenceStatus.Skipped
                    )
                )
                .OrderBy(o => o.ScheduledAt)
                .ThenBy(o => o.Id)
                .ToListAsync();
            return occurrences
                .Select(o => new AutoScheduleOccurrenceResponse(
                    o.Id,
                    o.ScheduledAt,
                    o.Status,
                    new MissionDefinitionResponse(definitions[o.MissionDefinitionId])
                ))
                .ToList();
        }

        public async Task<bool> SkipOccurrence(
            MissionDefinition missionDefinition,
            string occurrenceId
        )
        {
            await EnsureWriteAccess(missionDefinition);
            var now = timeProvider.GetUtcNow();
            var until = now.AddHours(24);
            var changed = await context
                .AutoScheduleOccurrences.Where(o =>
                    o.Id == occurrenceId
                    && o.MissionDefinitionId == missionDefinition.Id
                    && o.Status == AutoScheduleOccurrenceStatus.Scheduled
                    && o.ScheduledAt > now
                    && o.ScheduledAt <= until
                )
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(o => o.Status, AutoScheduleOccurrenceStatus.Skipped)
                );
            if (changed == 0)
            {
                logger.LogWarning("Cannot skip occurrence: it is no longer eligible");
                return false;
            }
            var occurrence = await context.AutoScheduleOccurrences.SingleAsync(o =>
                o.Id == occurrenceId
            );
            CancelJob(occurrence);
            Notify(missionDefinition);
            signalRService.ReportAutoScheduleToSignalR(
                "skipAutoMission",
                missionDefinition.Name,
                $"Skipped {missionDefinition.Name} scheduled for {occurrence.ScheduledAt:O}.",
                missionDefinition.InstallationCode
            );
            return true;
        }

        private void CancelJob(AutoScheduleOccurrence occurrence)
        {
            // IDs belong to in-memory Hangfire storage; never delete another process's ID.
            if (occurrence.JobId == null || occurrence.SchedulerInstance != SchedulerInstance)
                return;
            try
            {
                if (!backgroundJobs.Delete(occurrence.JobId))
                    logger.LogWarning(
                        "Could not delete job {Id}; its persisted occurrence prevents dispatch",
                        occurrence.JobId
                    );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Could not delete job {Id}; its persisted occurrence prevents dispatch",
                    occurrence.JobId
                );
            }
        }

        public async Task DispatchOccurrence(string occurrenceId)
        {
            var occurrence = await context.AutoScheduleOccurrences.SingleOrDefaultAsync(o =>
                o.Id == occurrenceId
            );
            if (occurrence == null)
            {
                logger.LogWarning("Ignoring job without persisted occurrence {Id}", occurrenceId);
                return;
            }
            var definition = await missionDefinitionService.ReadById(
                occurrence.MissionDefinitionId
            );
            if (definition == null)
            {
                logger.LogWarning(
                    "Mission definition for occurrence {Id} is no longer available",
                    occurrenceId
                );
                return;
            }
            await EnsureWriteAccess(definition);

            await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    // Each worker attempt owns its unit of work, including retries after rollback.
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    // Prevent definition edits while checking the schedule and creating its mission.
                    definition = await missionDefinitionService.ReadById(
                        occurrence.MissionDefinitionId,
                        forUpdate: true
                    );
                    if (definition == null)
                        return;
                    var now = timeProvider.GetUtcNow();
                    if (!await TryClaimOccurrence(occurrenceId, now))
                        return;

                    await CreateMissionForClaimedOccurrence(occurrence, definition, now);
                    await transaction.CommitAsync();
                });

            await NotifyAndStartDispatchedMission(occurrence.Id);
            if (definition != null)
                Notify(definition);
        }

        private async Task<bool> TryClaimOccurrence(string occurrenceId, DateTimeOffset now)
        {
            var changed = await context
                .AutoScheduleOccurrences.Where(o =>
                    o.Id == occurrenceId
                    && o.Status == AutoScheduleOccurrenceStatus.Scheduled
                    && o.ScheduledAt <= now
                )
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(o => o.Status, AutoScheduleOccurrenceStatus.Dispatched)
                );
            return changed != 0;
        }

        private async Task CreateMissionForClaimedOccurrence(
            AutoScheduleOccurrence occurrence,
            MissionDefinition definition,
            DateTimeOffset now
        )
        {
            var stillScheduled =
                definition
                    .AutoScheduleFrequency?.GetSchedulingInstants(
                        occurrence.ScheduledAt.AddSeconds(-1),
                        occurrence.ScheduledAt
                    )
                    .Contains(occurrence.ScheduledAt) == true;
            if (!stillScheduled)
            {
                await SetStatus(occurrence.Id, AutoScheduleOccurrenceStatus.Cancelled);
                return;
            }

            var robots = await robotService.ReadRobotsForInstallation(definition.InstallationCode);
            var robot = robots.FirstOrDefault(r =>
                r.CurrentInspectionAreaId == definition.InspectionArea.Id
            );
            if (robot == null)
            {
                await SetStatus(occurrence.Id, AutoScheduleOccurrenceStatus.Failed);
                var message = $"No robot available for auto scheduled mission {definition.Name}.";
                logger.LogWarning("{Message}", message);
                signalRService.ReportAutoScheduleToSignalR(
                    "AutoScheduleFail",
                    definition.Name,
                    message,
                    definition.InstallationCode
                );
                return;
            }

            await CreateQueuedMissionIfNeeded(occurrence, definition, robot, now);
        }

        private async Task CreateQueuedMissionIfNeeded(
            AutoScheduleOccurrence occurrence,
            MissionDefinition definition,
            Robot robot,
            DateTimeOffset now
        )
        {
            var queued = await missionRunService.ReadMissionRunQueue(robot.Id);
            if (queued.Any(m => m.MissionId == definition.Id))
                return;

            var mission = new MissionRun
            {
                Id = occurrence.Id,
                Name = definition.Name,
                Robot = robot,
                MissionId = definition.Id,
                Status = MissionStatus.Queued,
                CreationTime = now.UtcDateTime,
                Tasks = [.. definition.Tasks.Select(t => t.ToMissionRunTask())],
                InstallationCode = definition.InstallationCode,
                InspectionArea = definition.InspectionArea,
            };
            if (mission.Tasks.Any())
                mission.SetEstimatedTaskDuration();
            await missionRunService.Create(mission);
        }

        private async Task NotifyAndStartDispatchedMission(string occurrenceId)
        {
            // Retrying after a crash/failed start must not create another mission run.
            var created = await missionRunService.ReadById(occurrenceId);
            if (created != null)
            {
                _ = signalRService.SendMessageAsync(
                    "Mission run created",
                    created.InspectionArea.Installation,
                    new MissionRunResponse(created)
                );
                await missionSchedulingService.StartNextMissionRunIfSystemIsAvailable(
                    created.Robot
                );
            }
        }

        private Task<int> SetStatus(string id, AutoScheduleOccurrenceStatus status) =>
            context
                .AutoScheduleOccurrences.Where(o => o.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, status));

        private void Notify(MissionDefinition definition) =>
            _ = signalRService.SendMessageAsync(
                "Mission definition updated",
                definition.InspectionArea.Installation,
                new MissionDefinitionResponse(definition)
            );
    }
}
