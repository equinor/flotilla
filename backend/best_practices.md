# Backend best practices

Conventions and gotchas for working on the Flotilla backend. For installation and how to run the backend, see the [backend README](./README.md).

## Formatting

Formatting rules are defined in the [.editorconfig](../.editorconfig). We use [CSharpier](https://csharpier.com/) to auto-format on save (see [installation](https://csharpier.com/docs/About)). To format locally:

```bash
make format
```

CI enforces this — a PR fails if the code is not CSharpier-formatted.

## SignalR

We use SignalR to push event updates to the frontend via `SignalRService`. Event names must match what the frontend expects.

Do **not** await SignalR sends — in the current library version, awaiting from an async thread can cause the thread to silently exit without an exception. Let SignalR run after the current thread completes and ignore the await warning.

## Migrations

Any change to the model in [`api/Database/Models`](./api/Database/Models) requires a migration. Avoid adding a migration at the same time as someone else, since migrations are ordered and conflicts have to be resolved by regenerating them. See the [backend README](./README.md#database-migrations-ef-core) for the commands.
