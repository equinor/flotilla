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

Outside the test and local PostgreSQL-container paths, Staging and Production require token authentication for runtime PostgreSQL access. Synchronous token-setup failures stop startup without falling back to `Database:PostgreSqlConnectionString`; `Database:SeedExampleDataPostgres=true` is rejected before database setup because it uses that connection string. Token refresh behavior is unchanged. Development/local connection strings and password access (including pgAdmin), local example-data seeding, and the separate EF design-time/CI migration connection-string path remain supported.

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

Staging migrates the published release tag; Production migrates the version currently staged. Both image-update jobs now require successful migrations, including in legacy mode: a failed, cancelled or skipped migration prevents deployment. Image builds/copies can still run in parallel with migrations.

### Design-time migration authentication

The EF design-time factory does not boot `Program` or use runtime database authentication policy. An absent `Migrations__AuthenticationMode` (or explicit `Legacy`, case-insensitive) preserves the existing `Database:postgresConnectionString` lookup, falling back to the Key Vault secret `Database--PostgreSqlConnectionString` using `DefaultAzureCredential`. This design-time key differs from the runtime `Database:PostgreSqlConnectionString` key. Empty or unknown modes fail rather than falling back.

Explicit `AzureCli` (case-insensitive, no surrounding whitespace) selects only `AzureCliCredential` after the reusable workflow's dedicated-identity OIDC `azure/login`. It requires `Migrations__Postgres__Host` (single hostname/IP, no port), `Migrations__Postgres__Database`, `Migrations__Postgres__Username`, and an explicit tenant GUID in `AZURE_TENANT_ID`. Values must be nonempty without surrounding whitespace or control characters. It never reads password/Key Vault configuration or uses runtime `AzureAd` defaults. Connections use TLS `VerifyFull` and request `https://ossrdbms-aad.database.windows.net/.default` for each new physical connection, with a 30-second acquisition timeout and async cancellation. The design-time context owns and disposes its data source; EF migration locking and retry defaults are unchanged.

`api/.migration-auth-contract` contains exactly `azure-cli-postgresql-v1` followed by one LF. This is a reviewed capability attestation for the reusable workflow's pre-login checked-out-ref gate, not executable proof of authentication. A later approved opt-in uses workflow input `migration_auth_mode: azure_cli` and GitHub environment variables `MIGRATION_CLIENT_ID`, `MIGRATION_POSTGRES_HOST`, `MIGRATION_POSTGRES_DATABASE`, `MIGRATION_POSTGRES_USERNAME`, plus the existing tenant/subscription configuration.

**Preparation only:** no caller enables Azure CLI migration authentication yet, and no new migration is added. Opt-in requires a supported app release, approved infrastructure, a pre-provisioned database and a dedicated database principal with ownership/DDL/default privileges. Those roles have not been provisioned or verified here. Development/manual migrations, local/CI passwords, pgAdmin, runtime authentication and existing secrets are unchanged.

## Authentication

The backend validates access tokens against Microsoft Entra ID by default. `Authentication:Provider` selects the issuer: `EntraId` (default), or `Oidc` for any conformant OpenID Connect issuer given by `AzureAd:Authority`.

This is not a way to turn authentication off — issuer, audience, signature, lifetime and roles are validated under either value. An `http://` authority is accepted only in the `Local` and `IntegrationTest` environments and fails at startup anywhere else. Note that the frontend still signs in against Entra ID; only the backend is covered here.

### Running against a local Keycloak

```bash
make keycloak     # docker compose --profile keycloak up keycloak
```

Keycloak comes up on `http://localhost:8080` with the same realm the integration tests use. The realm is read from `../armada/robotics_integration_tests/custom_realms`; set `KEYCLOAK_REALM_DIR` if armada is not checked out beside this repository. Then add to `backend/api/.env`:

```
Authentication__Provider=Oidc
AzureAd__Authority=http://localhost:8080/realms/robotics
AzureAd__ClientId=flotilla-test
AzureAd__ClientSecret=flotilla-test-secret
Isar__Scopes__0=isar-api
Pointilla__Scopes__0=pointilla-api
```

The realm's `dev` user (password `dev`) holds `Role.Admin` and the per-installation roles. A token for calling the API directly, or from Swagger:

```bash
curl -s -X POST http://localhost:8080/realms/robotics/protocol/openid-connect/token \
  -d grant_type=client_credentials \
  -d client_id=integration-tests \
  -d client_secret=integration-tests-secret \
  -d scope=flotilla-api | jq -r .access_token
```

Request one `*-api` scope at a time: two audience mappers make Keycloak emit `aud` as an array, which ISAR rejects.

The armada integration tests run the backend against the same realm, under `ASPNETCORE_ENVIRONMENT=IntegrationTest`. That environment has no appsettings file here on purpose: its configuration is passed in as environment variables from [`armada/robotics_integration_tests/custom_containers/flotilla_backend.py`](https://github.com/equinor/armada/blob/main/robotics_integration_tests/custom_containers/flotilla_backend.py), alongside the realm that defines the clients and scopes. Change it there, not here.

## Monitoring

The backend is instrumented with [OpenTelemetry](https://opentelemetry.io/). Traces, metrics, and logs are exported via OTLP to a Grafana-compatible backend.

Locally, telemetry can be inspected in the Aspire dashboard at <http://localhost:18888>. It is started as part of `make compose`, or on its own together with the broker and the OpenTelemetry collector:

```bash
make broker-aspire
```
