# Flotilla

[![Backend](https://github.com/equinor/flotilla/actions/workflows/backend_lint_and_test.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/backend_lint_and_test.yml)
[![Frontend](https://github.com/equinor/flotilla/actions/workflows/lint_frontend_package.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/lint_frontend_package.yml)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/0b37a44f66044dbc81fff906344b476e)](https://www.codacy.com/gh/equinor/flotilla/dashboard?utm_source=github.com&utm_medium=referral&utm_content=equinor/flotilla&utm_campaign=Badge_Grade)

Flotilla is the main point of access for operators to interact with multiple robots in multiple facilities.
The application consists of a [frontend](frontend) in React, a [backend](backend) in ASP.NET, and a Mosquitto MQTT [broker](broker).

## Prerequisites

| Tool                                                                                                         | Version | Needed for                       |
| ------------------------------------------------------------------------------------------------------------ | ------- | -------------------------------- |
| [Docker](https://docs.docker.com/engine/install/) and [Docker Compose](https://docs.docker.com/compose/install/) | latest  | Full stack (`make compose`) + Tilt broker/Postgres |
| [Tilt](https://docs.tilt.dev/install.html)                                                                   | latest  | Running the stack locally (`make run`) |
| [uv](https://docs.astral.sh/uv/getting-started/installation/)                                                | latest  | Preflight + Key Vault scripts    |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)                                         | latest  | Fetching local secrets (`az login`) |
| [.NET SDK](https://dotnet.microsoft.com/download)                                                            | 10.x    | Running the backend              |
| [Node.js](https://github.com/nodesource/distributions)                                                       | 24.x    | Running the frontend             |
| [pnpm](https://pnpm.io/installation)                                                                         | latest  | Frontend package management      |
| `make`                                                                                                       | any     | The shorthand commands below     |

<details>
<summary>Installing make on MacOS</summary>

```bash
brew install make
```

</details>
<details>
<summary>Installing make on Windows</summary>

```bash
choco install make
```

</details>

## Quick start

For development, fork the repository first. Then clone it:

```bash
git clone https://github.com/equinor/flotilla
cd flotilla
```

Log in to Azure so the stack can fetch its local secrets from Key Vault, then
start everything with Tilt:

```bash
az login
make run          # runs preflight, then `tilt up`
```

`make run` brings up a self-contained local stack — MQTT broker and PostgreSQL
in Docker, and the backend and frontend as hot-reloading host processes:

| Service          | URL                                     |
| ---------------- | --------------------------------------- |
| Frontend         | <http://localhost:3001>                 |
| Backend Swagger  | <http://localhost:8000/swagger>         |
| Tilt UI          | <http://localhost:10350>                |

Stop it with `make down` (or `make clean` to also wipe the database volume).

Authentication uses Microsoft Entra ID, so `az login` is required; there is no
offline auth option yet. To drive a mission, run an [ISAR](https://github.com/equinor/isar)
robot on the host pointed at this stack's broker (`localhost:1883`) and backend
(`localhost:8000`).

The Tilt definition lives in [`tilt/`](tilt/) and is reused by the
[robotics](https://github.com/equinor/robotics) local-orchestration stack, which
imports `flotilla_stack()` from `tilt/flotilla.tilt` and runs it alongside SARA,
ISAR and the rest of the platform.

To run a single component instead, see the [frontend](frontend/README.md), [backend](backend/README.md), and [broker](broker/README.md) guides.

## Configuration

Each component reads its configuration from a `.env` file. The matching `.env.example` file is the source of truth for the available variables.

| Component | File                | Template                    | Notes                                                                 |
| --------- | ------------------- | --------------------------- | --------------------------------------------------------------------- |
| Frontend  | `frontend/.env`     | `frontend/.env.example`     | Variables must be prefixed with `VITE_` to reach the application.     |
| Backend   | `backend/api/.env`  | `backend/api/.env.example`  | Set `Local__DevUserId` to your own user id for local development.     |
| Broker    | `broker/.env`       | `broker/.env.example`       | `TLS_SERVER_KEY` is a secret and is found in our key vault.           |

> **Note:** these `.env` files configure the Docker Compose targets (`make broker`, `make keycloak`, …) and running a component directly. The Tilt path (`make run`) does not use them — it sets the backend and frontend environment itself and fetches secrets from Key Vault. The backend *container* started by compose does not load `backend/api/.env` either — [docker-compose.yml](./docker-compose.yml) reads `AZURE_CLIENT_SECRET` from a `.env` file in the repository root, which `setup.sh` does not create. To run the full stack against Azure AD you currently have to add `AZURE_CLIENT_SECRET=...` to a root `.env` yourself.

## Other make commands

`make run` is the recommended way to develop locally. The remaining targets in
the [root Makefile](./Makefile) drive the production-shaped container images
through Docker Compose (independent of the Tilt path, still configured via
`./setup.sh` and the per-component `.env` files):

```bash
make compose         # run the full stack in Docker
make broker          # run only the MQTT broker
make broker-aspire   # run the broker, OpenTelemetry collector, and Aspire dashboard
make keycloak        # run a local OpenID Connect issuer instead of Entra ID
```

Per-component commands live in the [backend Makefile](./backend/Makefile) and
[frontend Makefile](./frontend/Makefile).

`make keycloak` starts the same realm the integration tests use, so the backend can be run without Entra ID. It needs a little configuration in `backend/api/.env` — see [Running against a local Keycloak](backend/README.md#running-against-a-local-keycloak).

## Deployments

We currently have 3 environments (Development, Staging, and Production) deployed to AKS under `robotics.equinor.com`.

| Environment | Deployment                                                                                                                        | Status                                                                                                                                                                                     |
| ----------- | --------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Development | [Frontend](https://flotilla.dev.robotics.equinor.com/)<br>[Backend](https://flotilla.dev.robotics.equinor.com/api/swagger)         | [![Dev](https://github.com/equinor/flotilla/actions/workflows/deploy_to_development.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/deploy_to_development.yml)        |
| Staging     | [Frontend](https://flotilla.staging.robotics.equinor.com/)<br>[Backend](https://flotilla.staging.robotics.equinor.com/api/swagger) | [![Staging](https://github.com/equinor/flotilla/actions/workflows/deploy_to_staging.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/deploy_to_staging.yml)            |
| Production  | [Frontend](https://flotilla.robotics.equinor.com/)<br>[Backend](https://flotilla.robotics.equinor.com/api/swagger)                 | [![Production](https://github.com/equinor/flotilla/actions/workflows/promote_to_production.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/promote_to_production.yml) |

## Contributions

Equinor welcomes all kinds of contributions, including code, bug reports, issues, feature requests, and documentation.
Please initiate your contribution by creating an [issue](https://github.com/equinor/flotilla/issues) or by forking the
project and making a pull request. Commit messages shall be written according to [this guide](https://cbea.ms/git-commit/).
