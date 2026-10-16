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
        Task<AutoScheduleFrequency?> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            IList<TimeAndDay>? newAutoScheduleFrequency
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

        private async Task EnsureWriteAccess(MissionDefinition missionDefinition)
        {
            var allowed = await accessRoleService.GetAllowedInstallationCodes(AccessMode.Write);
            if (
                !allowed.Contains(
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

        private async Task LockDefinition(string id)
        {
            // Serialize reconciliation, edits and dispatch across backend instances.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM "MissionDefinitions" WHERE "Id" = {id} FOR UPDATE"""
            );
        }

        public async Task Reconcile(MissionDefinition missionDefinition)
        {
            await EnsureWriteAccess(missionDefinition);
            await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    await LockDefinition(missionDefinition.Id);
                    var current = await missionDefinitionService.ReadById(missionDefinition.Id);
                    if (current == null)
                        return;
                    var cancelled = await ReconcileLocked(current);
                    await transaction.CommitAsync();
                    cancelled.ForEach(CancelJob);
                    Notify(current);
                });
        }

        private async Task<List<AutoScheduleOccurrence>> ReconcileLocked(
            MissionDefinition missionDefinition
        )
        {
            var cancelled = new List<AutoScheduleOccurrence>();
            var now = timeProvider.GetUtcNow();
            var desired =
                missionDefinition
                    .AutoScheduleFrequency?.GetSchedulingInstants(now, now.AddHours(48))
                    .ToHashSet()
                ?? [];
            var occurrences = await context
                .AutoScheduleOccurrences.Where(o =>
                    o.MissionDefinitionId == missionDefinition.Id
                    && (o.ScheduledAt > now || o.Status == AutoScheduleOccurrenceStatus.Scheduled)
                )
                .ToListAsync();

            foreach (
                var occurrence in occurrences.Where(o =>
                    o.Status == AutoScheduleOccurrenceStatus.Scheduled
                )
            )
            {
                if (
                    occurrence.ScheduledAt <= now
                    && occurrence.SchedulerInstance == SchedulerInstance
                )
                    continue;
                if (occurrence.ScheduledAt <= now || !desired.Contains(occurrence.ScheduledAt))
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
                    cancelled.Add(occurrence);
                    occurrence.Status = status;
                }
            }

            foreach (var instant in desired.Order())
            {
                var occurrence = occurrences.SingleOrDefault(o => o.ScheduledAt == instant);
                if (occurrence == null)
                {
                    occurrence = new AutoScheduleOccurrence
                    {
                        MissionDefinitionId = missionDefinition.Id,
                        ScheduledAt = instant,
                        Status = AutoScheduleOccurrenceStatus.Scheduled,
                    };
                    context.AutoScheduleOccurrences.Add(occurrence);
                    await context.SaveChangesAsync();
                    context.Entry(occurrence).State = EntityState.Detached;
                }
                else if (occurrence.Status == AutoScheduleOccurrenceStatus.Cancelled)
                {
                    // Re-adding a removed weekly slot restores it, but never restores a skip.
                    await context
                        .AutoScheduleOccurrences.Where(o => o.Id == occurrence.Id)
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(o => o.Status, AutoScheduleOccurrenceStatus.Scheduled)
                                .SetProperty(o => o.SchedulerInstance, (string?)null)
                        );
                    occurrence.Status = AutoScheduleOccurrenceStatus.Scheduled;
                    occurrence.SchedulerInstance = null;
                }

                if (
                    occurrence.Status != AutoScheduleOccurrenceStatus.Scheduled
                    || occurrence.SchedulerInstance == SchedulerInstance
                )
                    continue;

                var jobId = backgroundJobs.Schedule(
                    () => DispatchOccurrence(occurrence.Id),
                    instant
                );
                try
                {
                    await context
                        .AutoScheduleOccurrences.Where(o =>
                            o.Id == occurrence.Id
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
                            MissionDefinitionId = missionDefinition.Id,
                            JobId = jobId,
                            SchedulerInstance = SchedulerInstance,
                        }
                    );
                    throw;
                }
            }
            return cancelled;
        }

        public async Task<AutoScheduleFrequency?> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            IList<TimeAndDay>? newAutoScheduleFrequency
        )
        {
            await EnsureWriteAccess(missionDefinition);
            if (newAutoScheduleFrequency?.Count == 0)
                newAutoScheduleFrequency = null;

            await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    await LockDefinition(missionDefinition.Id);
                    missionDefinition.AutoScheduleFrequency =
                        newAutoScheduleFrequency == null
                            ? null
                            : new AutoScheduleFrequency
                            {
                                SchedulingTimesCETperWeek = newAutoScheduleFrequency,
                            };
                    await missionDefinitionService.Update(missionDefinition);
                    var cancelled = await ReconcileLocked(missionDefinition);
                    await transaction.CommitAsync();
                    cancelled.ForEach(CancelJob);
                    Notify(missionDefinition);
                });
            return missionDefinition.AutoScheduleFrequency;
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
                logger.LogWarning(
                    "Cannot skip occurrence {Id}: it is no longer eligible",
                    occurrenceId
                );
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
                    await LockDefinition(occurrence.MissionDefinitionId);
                    definition = await missionDefinitionService.ReadById(
                        occurrence.MissionDefinitionId
                    );
                    if (definition == null)
                        return;
                    var now = timeProvider.GetUtcNow();
                    var changed = await context
                        .AutoScheduleOccurrences.Where(o =>
                            o.Id == occurrenceId
                            && o.Status == AutoScheduleOccurrenceStatus.Scheduled
                            && o.ScheduledAt <= now
                        )
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(o => o.Status, AutoScheduleOccurrenceStatus.Dispatched)
                        );
                    if (changed == 0)
                        return;

                    var stillScheduled =
                        definition
                            .AutoScheduleFrequency?.GetSchedulingInstants(
                                occurrence.ScheduledAt.AddSeconds(-1),
                                occurrence.ScheduledAt
                            )
                            .Contains(occurrence.ScheduledAt) == true;
                    if (!stillScheduled)
                    {
                        await SetStatus(occurrenceId, AutoScheduleOccurrenceStatus.Cancelled);
                    }
                    else
                    {
                        var robots = await robotService.ReadRobotsForInstallation(
                            definition.InstallationCode
                        );
                        var robot = robots.FirstOrDefault(r =>
                            r.CurrentInspectionAreaId == definition.InspectionArea.Id
                        );
                        if (robot == null)
                        {
                            await SetStatus(occurrenceId, AutoScheduleOccurrenceStatus.Failed);
                            var message =
                                $"No robot available for auto scheduled mission {definition.Name}.";
                            logger.LogWarning("{Message}", message);
                            signalRService.ReportAutoScheduleToSignalR(
                                "AutoScheduleFail",
                                definition.Name,
                                message,
                                definition.InstallationCode
                            );
                        }
                        else
                        {
                            var queued = await missionRunService.ReadMissionRunQueue(robot.Id);
                            if (!queued.Any(m => m.MissionId == definition.Id))
                            {
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
                        }
                    }
                    await transaction.CommitAsync();
                });

            // Retrying after a crash/failed start must not create another mission run.
            var created = await missionRunService.ReadById(occurrence.Id);
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
            if (definition != null)
                Notify(definition);
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
