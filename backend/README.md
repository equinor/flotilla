# Flotilla backend

The backend of Flotilla is an ASP.NET application. See the [.NET documentation](https://docs.microsoft.com/en-us/dotnet/fundamentals/) for framework concepts.

For general setup (environment, Docker, database connection), see the [root README](../README.md).

## Common commands

Common commands are defined in the [Makefile](./Makefile):

```bash
make run       # dotnet run --project api
make build     # dotnet build api
make test      # dotnet test
make format    # dotnet csharpier format .
make migration name=AddSomething  # create a new EF Core migration
```

## Database migrations (EF Core)

The database model lives in [`api/Database/Models`](./api/Database/Models) and we use [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) as an O/RM. When changing the model, add a [migration](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/).

Install the EF Core CLI once:

```bash
dotnet tool install --global dotnet-ef
```

Create a new migration (make sure you have the latest `main` and that no one else is adding a migration at the same time):

```bash
make migration name=AddTableNamePropertyName
```

This adds files under `backend/api/Migrations` that must be committed. `add` does not modify any database — it only describes the changes.

To discard a migration you're not happy with:

```bash
dotnet ef migrations remove
```

### Applying migrations

- **Development**: after merging a PR that touches `backend/api/Migrations`, manually run the ["Run database migrations (Development)"](https://github.com/equinor/flotilla/actions/workflows/run_development_migrations.yml) workflow.
- **Staging / Production**: applied automatically by the [deploy_to_staging](https://github.com/equinor/flotilla/blob/main/.github/workflows/deploy_to_staging.yml) and [promote_to_production](https://github.com/equinor/flotilla/blob/main/.github/workflows/promote_to_production.yml) workflows.

## Formatting

Formatting rules are defined in the [.editorconfig](../.editorconfig). We use [CSharpier](https://csharpier.com/) to auto-format on save (see [installation](https://csharpier.com/docs/About)). To check formatting locally:

```bash
make format
```

## SignalR

We use SignalR to push event updates to the frontend via `SignalRService`. Event names must match what the frontend expects.

Do **not** await SignalR sends — in the current library version, awaiting from an async thread can cause the thread to silently exit without an exception. Let SignalR run after the current thread completes and ignore the await warning.

## Monitoring

The backend is instrumented with [OpenTelemetry](https://opentelemetry.io/). Traces, metrics, and logs are exported via OTLP to a Grafana-compatible backend. Locally, telemetry can be inspected in the Aspire dashboard (see the [root README](../README.md#using-the-aspire-dashboard)).
