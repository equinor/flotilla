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
        Task<IList<ScheduledMissionRunResponse>> ReadUpcoming(string installationCode);
        Task<bool> SkipMissionRun(MissionDefinition missionDefinition, string missionRunId);
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

        private async Task EnsureWriteAccess(MissionDefinition definition)
        {
            var allowed = await accessRoleService.GetAllowedInstallationCodes(AccessMode.Write);
            if (!allowed.Contains(definition.InstallationCode, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Denied auto scheduling change for mission definition {Id}",
                    definition.Id
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
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    var current = await missionDefinitionService.ReadById(
                        missionDefinition.Id,
                        forUpdate: true
                    );
                    if (current == null)
                    {
                        logger.LogInformation(
                            "Skipping reconciliation: mission definition is no longer available"
                        );
                        return;
                    }
                    var cancelled = await ReconcileLocked(current);
                    await transaction.CommitAsync();
                    cancelled.ForEach(CancelJob);
                    Notify(current);
                });
        }

        private async Task<List<MissionRun>> ReconcileLocked(MissionDefinition definition)
        {
            var now = timeProvider.GetUtcNow();
            var desired =
                definition
                    .AutoScheduleFrequency?.GetSchedulingInstants(
                        now,
                        now.Add(ReconciliationHorizon)
                    )
                    .ToHashSet()
                ?? [];
            var runs = await context
                .MissionRuns.Where(r =>
                    r.MissionId == definition.Id
                    && r.ScheduledAt != null
                    && (r.ScheduledAt > now || r.Status == MissionStatus.Scheduled)
                )
                .ToListAsync();
            var cancelled = new List<MissionRun>();
            foreach (var run in runs.Where(r => r.Status == MissionStatus.Scheduled))
            {
                if (run.ScheduledAt <= now && run.SchedulerInstance == SchedulerInstance)
                    continue;
                if (run.ScheduledAt <= now || !desired.Contains(run.ScheduledAt!.Value))
                {
                    run.Status =
                        run.ScheduledAt <= now ? MissionStatus.Missed : MissionStatus.Cancelled;
                    await SetInactiveStatus(run.Id, run.Status, run.EndTime);
                    cancelled.Add(run);
                }
            }
            foreach (var instant in desired.Order())
            {
                var run = runs.SingleOrDefault(r => r.ScheduledAt == instant);
                if (run == null)
                {
                    run = new MissionRun
                    {
                        Id = Guid.NewGuid().ToString(),
                        MissionId = definition.Id,
                        Name = definition.Name,
                        InstallationCode = definition.InstallationCode,
                        InspectionArea = definition.InspectionArea,
                        CreationTime = now.UtcDateTime,
                        ScheduledAt = instant,
                        Status = MissionStatus.Scheduled,
                        Tasks = [],
                    };
                    context.Entry(run.InspectionArea).State = EntityState.Unchanged;
                    context.MissionRuns.Add(run);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                }
                else if (run.Status == MissionStatus.Cancelled && !run.IsDeprecated)
                {
                    // Re-adding a weekly slot restores cancellations, never an explicit skip.
                    run.Status = MissionStatus.Scheduled;
                    run.SchedulerInstance = null;
                    await context
                        .MissionRuns.Where(r => r.Id == run.Id)
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(r => r.Status, MissionStatus.Scheduled)
                                .SetProperty(r => r.EndTime, (DateTime?)null)
                                .SetProperty(r => r.SchedulerInstance, (string?)null)
                        );
                }
                if (
                    run.Status == MissionStatus.Scheduled
                    && !run.IsDeprecated
                    && run.SchedulerInstance != SchedulerInstance
                )
                    await ScheduleActivation(run);
            }
            return cancelled;
        }

        private async Task ScheduleActivation(MissionRun run)
        {
            var jobId = backgroundJobs.Schedule(
                () => ActivateMissionRun(run.Id),
                run.ScheduledAt!.Value
            );
            try
            {
                await context
                    .MissionRuns.Where(r => r.Id == run.Id && r.Status == MissionStatus.Scheduled)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(r => r.ScheduleJobId, jobId)
                            .SetProperty(r => r.SchedulerInstance, SchedulerInstance)
                    );
            }
            catch
            {
                run.ScheduleJobId = jobId;
                run.SchedulerInstance = SchedulerInstance;
                CancelJob(run);
                throw;
            }
        }

        public async Task<AutoScheduleFrequency?> UpdateAutoScheduleFrequency(
            MissionDefinition missionDefinition,
            IList<TimeAndDay>? newAutoScheduleFrequency
        )
        {
            await EnsureWriteAccess(missionDefinition);
            await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    var current = await missionDefinitionService.ReadById(
                        missionDefinition.Id,
                        accessMode: AccessMode.Write,
                        forUpdate: true
                    );
                    if (current == null)
                        throw new InvalidOperationException(
                            "Mission definition is no longer available for schedule changes."
                        );
                    missionDefinition.AutoScheduleFrequency = newAutoScheduleFrequency
                        is not { Count: > 0 }
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

        public async Task<IList<ScheduledMissionRunResponse>> ReadUpcoming(string installationCode)
        {
            var definitions = (
                await missionDefinitionService.ReadByInstallationCode(installationCode)
            ).ToDictionary(d => d.Id);
            var ids = definitions.Keys.ToList();
            var now = timeProvider.GetUtcNow();
            var until = now.AddHours(24);
            var runs = await context
                .MissionRuns.Where(r =>
                    ids.Contains(r.MissionId)
                    && !r.IsDeprecated
                    && r.ScheduledAt > now
                    && r.ScheduledAt <= until
                    && (r.Status == MissionStatus.Scheduled || r.Status == MissionStatus.Skipped)
                )
                .OrderBy(r => r.ScheduledAt)
                .ThenBy(r => r.Id)
                .ToListAsync();
            return runs.Select(r => new ScheduledMissionRunResponse(
                    r.Id,
                    r.ScheduledAt!.Value,
                    r.Status,
                    new MissionDefinitionResponse(definitions[r.MissionId])
                ))
                .ToList();
        }

        public async Task<bool> SkipMissionRun(
            MissionDefinition missionDefinition,
            string missionRunId
        )
        {
            await EnsureWriteAccess(missionDefinition);
            var now = timeProvider.GetUtcNow();
            var until = now.AddHours(24);
            var changed = await context
                .MissionRuns.Where(r =>
                    r.Id == missionRunId
                    && r.MissionId == missionDefinition.Id
                    && r.Status == MissionStatus.Scheduled
                    && !r.IsDeprecated
                    && r.ScheduledAt > now
                    && r.ScheduledAt <= until
                )
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(r => r.Status, MissionStatus.Skipped)
                        .SetProperty(r => r.EndTime, now.UtcDateTime)
                );
            if (changed == 0)
            {
                logger.LogWarning("Cannot skip scheduled mission run: it is no longer eligible");
                return false;
            }
            var run = await context.MissionRuns.SingleAsync(r => r.Id == missionRunId);
            CancelJob(run);
            Notify(missionDefinition);
            signalRService.ReportAutoScheduleToSignalR(
                "skipAutoMission",
                missionDefinition.Name,
                $"Skipped {missionDefinition.Name} scheduled for {run.ScheduledAt:O}.",
                missionDefinition.InstallationCode
            );
            return true;
        }

        private void CancelJob(MissionRun run)
        {
            // In-memory Hangfire IDs can be reused after restart.
            if (run.ScheduleJobId == null || run.SchedulerInstance != SchedulerInstance)
                return;
            try
            {
                if (!backgroundJobs.Delete(run.ScheduleJobId))
                    logger.LogWarning(
                        "Could not delete job {Id}; persisted run state prevents activation",
                        run.ScheduleJobId
                    );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Could not delete job {Id}; persisted run state prevents activation",
                    run.ScheduleJobId
                );
            }
        }

        public async Task ActivateMissionRun(string missionRunId)
        {
            var run = await context.MissionRuns.SingleOrDefaultAsync(r => r.Id == missionRunId);
            if (run == null)
            {
                // A very near-due job can race the transaction which created its run.
                throw new InvalidOperationException("Scheduled mission run is not yet available.");
            }
            var definition = await missionDefinitionService.ReadById(run.MissionId);
            if (definition == null)
            {
                logger.LogInformation("Ignoring scheduled run for unavailable mission definition");
                return;
            }
            await EnsureWriteAccess(definition);
            var processedByThisJob = false;
            await context
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    processedByThisJob = false;
                    context.ChangeTracker.Clear();
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    definition = await missionDefinitionService.ReadById(
                        run.MissionId,
                        forUpdate: true
                    );
                    if (definition == null)
                        return;
                    var now = timeProvider.GetUtcNow();
                    // Conditional update locks the row against skips until activation commits.
                    var claimed = await context
                        .MissionRuns.Where(r =>
                            r.Id == missionRunId
                            && r.Status == MissionStatus.Scheduled
                            && !r.IsDeprecated
                            && r.ScheduledAt <= now
                            && r.SchedulerInstance == SchedulerInstance
                        )
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(r => r.Status, MissionStatus.Scheduled)
                        );
                    if (claimed == 0)
                        return;
                    run = await context
                        .MissionRuns.AsTracking()
                        .SingleAsync(r => r.Id == missionRunId);
                    var stillScheduled =
                        definition
                            .AutoScheduleFrequency?.GetSchedulingInstants(
                                run.ScheduledAt!.Value.AddSeconds(-1),
                                run.ScheduledAt.Value
                            )
                            .Contains(run.ScheduledAt.Value) == true;
                    if (!stillScheduled)
                        run.Status = MissionStatus.Cancelled;
                    else
                        await ActivateLocked(run, definition, now);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    processedByThisJob = true;
                });
            context.ChangeTracker.Clear();
            // Once committed, normal queue processing also recovers a crash before this wake-up.
            if (processedByThisJob && run is { Status: MissionStatus.Queued, Robot: not null })
            {
                _ = signalRService.SendMessageAsync(
                    "Mission run created",
                    run.InspectionArea.Installation,
                    new MissionRunResponse(run)
                );
                await missionSchedulingService.StartNextMissionRunIfSystemIsAvailable(run.Robot);
            }
            else if (processedByThisJob && run.Status == MissionStatus.Failed && definition != null)
            {
                signalRService.ReportAutoScheduleToSignalR(
                    "AutoScheduleFail",
                    definition.Name,
                    run.StatusReason!,
                    definition.InstallationCode
                );
            }
            if (definition != null)
                Notify(definition);
        }

        private async Task ActivateLocked(
            MissionRun run,
            MissionDefinition definition,
            DateTimeOffset now
        )
        {
            var robots = await robotService.ReadRobotsForInstallation(definition.InstallationCode);
            var robot = robots.FirstOrDefault(r =>
                r.CurrentInspectionAreaId == definition.InspectionArea.Id
            );
            if (robot == null)
            {
                run.Status = MissionStatus.Failed;
                run.StatusReason = "No robot available in the scheduled mission's inspection area.";
                logger.LogWarning("No robot available for scheduled mission {Id}", definition.Id);
                return;
            }
            if (
                (await missionRunService.ReadMissionRunQueue(robot.Id)).Any(r =>
                    r.MissionId == definition.Id
                )
            )
            {
                run.Status = MissionStatus.Cancelled;
                run.StatusReason = "This mission definition already has a queued run.";
                return;
            }
            context.Entry(robot).State = EntityState.Unchanged;
            context.Entry(definition.InspectionArea).State = EntityState.Unchanged;
            run.Robot = robot;
            run.Tasks = [.. definition.Tasks.Select(t => t.ToMissionRunTask())];
            run.Name = definition.Name;
            run.InstallationCode = definition.InstallationCode;
            run.InspectionArea = definition.InspectionArea;
            if (missionRunService.IncludesUnsupportedInspectionType(run))
            {
                run.Status = MissionStatus.Failed;
                run.StatusReason = "Scheduled mission contains unsupported inspection types.";
                run.Robot = null;
                run.Tasks = [];
                logger.LogWarning(
                    "Unsupported inspection types for scheduled mission {Id}",
                    definition.Id
                );
                return;
            }
            run.SetEstimatedTaskDuration();
            run.ActivatedAt = now;
            run.Status = MissionStatus.Queued;
        }

        private Task<int> SetInactiveStatus(string id, MissionStatus status, DateTime? endTime) =>
            context
                .MissionRuns.Where(r => r.Id == id && r.Status == MissionStatus.Scheduled)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(r => r.Status, status).SetProperty(r => r.EndTime, endTime)
                );

        private void Notify(MissionDefinition definition) =>
            _ = signalRService.SendMessageAsync(
                "Mission definition updated",
                definition.InspectionArea.Installation,
                new MissionDefinitionResponse(definition)
            );
    }
}
