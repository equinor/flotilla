# Flotilla

[![Backend](https://github.com/equinor/flotilla/actions/workflows/backend_lint_and_test.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/backend_lint_and_test.yml)
[![Frontend](https://github.com/equinor/flotilla/actions/workflows/frontend_lint_and_test.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/frontend_lint_and_test.yml)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/0b37a44f66044dbc81fff906344b476e)](https://www.codacy.com/gh/equinor/flotilla/dashboard?utm_source=github.com&utm_medium=referral&utm_content=equinor/flotilla&utm_campaign=Badge_Grade)

Flotilla is the main point of access for operators to interact with multiple robots in multiple facilities.  
The application consists of a [frontend](frontend) in React, a [backend](backend) in ASP.NET and a Mosquitto MQTT [Broker](broker).

## Deployments

We currently have 3 environment (Development, Staging and Production) deployed to Aurora.

| Environment | Deployment                                                                                                                                                | Status                                                                                                                                                                                      |
| ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Development | [Frontend](https://shared.dev.aurora.equinor.com/robotics-frontend/)<br>[Backend](https://shared.dev.aurora.equinor.com/robotics-backend/swagger)         | [![Dev](https://github.com/equinor/flotilla/actions/workflows/deploy_to_development.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/deploy_to_development.yml)        |
| Staging     | [Frontend](https://shared.aurora.equinor.com/robotics-staging-frontend/)<br>[Backend](https://shared.aurora.equinor.com/robotics-staging-backend/swagger) | [![Staging](https://github.com/equinor/flotilla/actions/workflows/deploy_to_staging.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/deploy_to_staging.yml)            |
| Production  | [Frontend](https://shared.aurora.equinor.com/robotics-prod-frontend/)<br>[Backend](https://shared.aurora.equinor.com/robotics-prod-backend/swagger)       | [![Production](https://github.com/equinor/flotilla/actions/workflows/promote_to_production.yml/badge.svg)](https://github.com/equinor/flotilla/actions/workflows/promote_to_production.yml) |

## Setup

For development, please fork the repository. Then, clone the repository:

```
git clone https://github.com/equinor/flotilla
```

Please see separate installation guides for [frontend](frontend), [backend](backend), and [Broker](broker).
For the environment setup, either run the script as described below or do it manually as described in each component.

### Automatic environment setup

Run the [setup.sh](./setup.sh) to automatically set up your dev environment for the components.
This script will ask you for the `Client Secret` for the backend and the `MQTT broker server key` for the MQTT broker.

## Run with docker

Install [docker](https://docs.docker.com/engine/install/ubuntu/) and [docker compose](https://docs.docker.com/compose/install/).

Build the docker container:

```
docker compose build
```

Setup a .env file in the backend directory with the following environment variables:

```
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_CLIENT_SECRET
```

Start Flotilla by running:

```
docker compose up
```

or

```
docker compose up --build
```

## Contributions

Equinor welcomes all kinds of contributions, including code, bug reports, issues, feature requests, and documentation.
Please initiate your contribution by creating an [issue](https://github.com/equinor/isar/issues) or by forking the
project and making a pull requests. Commit messages shall be written according to [this guide](https://cbea.ms.git-commit/).
