# Flotilla

[![Backend](https://github.com/equinor/flotilla/actions/workflows/backend_lint_and_test.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/backend_lint_and_test.yml)
[![Frontend](https://github.com/equinor/flotilla/actions/workflows/lint_frontend_package.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/lint_frontend_package.yml)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/0b37a44f66044dbc81fff906344b476e)](https://www.codacy.com/gh/equinor/flotilla/dashboard?utm_source=github.com&utm_medium=referral&utm_content=equinor/flotilla&utm_campaign=Badge_Grade)

Flotilla is the main point of access for operators to interact with multiple robots in multiple facilities.
The application consists of a [frontend](frontend) in React, a [backend](backend) in ASP.NET, and a Mosquitto MQTT [broker](broker).

## Prerequisites

| Tool                                                                                                         | Version | Needed for                       |
| ------------------------------------------------------------------------------------------------------------ | ------- | -------------------------------- |
| [Docker](https://docs.docker.com/engine/install/) and [Docker Compose](https://docs.docker.com/compose/install/) | latest  | Running the full stack           |
| [.NET SDK](https://dotnet.microsoft.com/download)                                                            | 10.x    | Running the backend directly     |
| [Node.js](https://github.com/nodesource/distributions)                                                       | 24.x    | Running the frontend directly    |
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

Create the local configuration files:

```bash
./setup.sh
```

The script creates `frontend/.env`, `backend/api/.env`, and `broker/.env` from their `.env.example` files. It prompts you for the **MQTT broker server key**, which is found in our key vault. Everything else can be configured manually afterwards — see [Configuration](#configuration).

Start the full stack:

```bash
make compose      # docker compose up --build
```

| Service          | URL                                     |
| ---------------- | --------------------------------------- |
| Frontend         | <http://localhost:3001>                 |
| Backend Swagger  | <http://localhost:8000/swagger>         |
| Aspire dashboard | <http://localhost:18888>                |

To run a single component instead, see the [frontend](frontend/README.md), [backend](backend/README.md), and [broker](broker/README.md) guides.

## Configuration

Each component reads its configuration from a `.env` file. The matching `.env.example` file is the source of truth for the available variables.

| Component | File                | Template                    | Notes                                                                 |
| --------- | ------------------- | --------------------------- | --------------------------------------------------------------------- |
| Frontend  | `frontend/.env`     | `frontend/.env.example`     | Variables must be prefixed with `VITE_` to reach the application.     |
| Backend   | `backend/api/.env`  | `backend/api/.env.example`  | Set `Local__DevUserId` to your own user id for local development.     |
| Broker    | `broker/.env`       | `broker/.env.example`       | `TLS_SERVER_KEY` is a secret and is found in our key vault.           |

## Other make commands

Common commands are defined in the [root Makefile](./Makefile), the [backend Makefile](./backend/Makefile), and the [frontend Makefile](./frontend/Makefile):

```bash
make compose         # run the full stack in Docker
make broker          # run only the MQTT broker
make broker-aspire   # run the broker, OpenTelemetry collector, and Aspire dashboard
```

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
