# Backend best practices

Conventions and gotchas for working on the Flotilla backend. For installation and how to run the backend, see the [backend README](./README.md).

## Formatting

Formatting is handled entirely by [CSharpier](https://csharpier.com/), which is opinionated and takes no configuration. The version is pinned in [.config/dotnet-tools.json](./.config/dotnet-tools.json). We recommend setting it up to format on save (see [installation](https://csharpier.com/docs/About)). To format locally:

```bash
make format
```

CI enforces this — a PR fails if the code is not CSharpier-formatted.

## SignalR

We use SignalR to push event updates to the frontend via `SignalRService`. Event names must match what the frontend expects.

Do **not** await SignalR sends — in the current library version, awaiting from an async thread can cause the thread to silently exit without an exception. Let SignalR run after the current thread completes and ignore the await warning.

## Concurrent mission and task updates

MQTT mission and task handlers can run concurrently. Read mission runs with tracking before modifying them and save only the detected changes; do not use `context.Update(missionRun)` on the loaded graph. That can overwrite a task's newer status and timestamps with the mission handler's stale copy. Re-read the mission after saving before building its SignalR response so the response includes task updates committed during the save.

When filtering excluded tasks before scheduling, remove them by ID through `MissionRunService.RemoveTasks` rather than replacing the tracked task collection with detached copies.

## Migrations

Any change to the model in [`api/Database/Models`](./api/Database/Models) requires a migration. Avoid adding a migration at the same time as someone else, since migrations are ordered and conflicts have to be resolved by regenerating them. See the [backend README](./README.md#database-migrations-ef-core) for the commands.
