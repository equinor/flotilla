# Flotilla backend

The backend of Flotilla is an ASP.NET application. See the [.NET documentation](https://docs.microsoft.com/en-us/dotnet/fundamentals/) for framework concepts.

For development conventions and gotchas, see [best_practices.md](./best_practices.md).

## Prerequisites

- [.NET SDK 10.x](https://dotnet.microsoft.com/download)
- [`dotnet-ef`](https://docs.microsoft.com/en-us/ef/core/cli/dotnet), if you need to work with migrations:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Configuration

The backend reads environment variables from `backend/api/.env`. Create it by running [`setup.sh`](../setup.sh) from the repository root, or by copying the template manually:

```bash
cp api/.env.example api/.env
```

Set `Local__DevUserId` to your own user id for local development.

## Run

Common commands are defined in the [Makefile](./Makefile):

```bash
make run       # dotnet run --project api
make build     # dotnet build api
make test      # dotnet test
make format    # dotnet csharpier format .
```

Swagger is served at <http://localhost:8000/swagger>.

To run the backend in Docker together with the rest of the stack, see the [root README](../README.md#quick-start).

## Connecting to the development database

By default the backend runs against an in-memory database. To use the development database instead, add the following to `backend/api/.env`:

```bash
Database__UseInMemoryDatabase=false
Database__PostgreSqlConnectionString=...
```

The connection string is found in the key vault in the development resource group in Azure. Remember to add your IP address to the accepted IPs for connecting to the database.

## Database migrations (EF Core)

The database model lives in [`api/Database/Models`](./api/Database/Models) and we use [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) as an O/RM. When changing the model, add a [migration](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/).

Create a new migration (make sure you have the latest `main` and that no one else is adding a migration at the same time):

```bash
make migration name=AddTableNamePropertyName
```

This adds files under `backend/api/Migrations` that must be committed. Adding a migration does not modify any database — it only describes the changes.

To discard a migration you're not happy with:

```bash
dotnet ef migrations remove
```

### Applying migrations

- **Development**: after merging a PR that touches `backend/api/Migrations`, manually run the ["Run database migrations (Development)"](https://github.com/equinor/flotilla/actions/workflows/run_development_migrations.yml) workflow.
- **Staging / Production**: applied automatically by the [deploy_to_staging](https://github.com/equinor/flotilla/blob/main/.github/workflows/deploy_to_staging.yml) and [promote_to_production](https://github.com/equinor/flotilla/blob/main/.github/workflows/promote_to_production.yml) workflows.

## Monitoring

The backend is instrumented with [OpenTelemetry](https://opentelemetry.io/). Traces, metrics, and logs are exported via OTLP to a Grafana-compatible backend.

Locally, telemetry can be inspected in the Aspire dashboard at <http://localhost:18888>. It is started as part of `make compose`, or on its own together with the broker and the OpenTelemetry collector:

```bash
make broker-aspire
```
