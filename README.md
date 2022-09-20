# Flotilla

![Backend](https://github.com/equinor/flotilla/actions/workflows/backend_lint_and_test.yml/badge.svg)
![Frontend](https://github.com/equinor/flotilla/actions/workflows/frontend_lint_and_test.yml/badge.svg)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/0b37a44f66044dbc81fff906344b476e)](https://www.codacy.com/gh/equinor/flotilla/dashboard?utm_source=github.com&utm_medium=referral&utm_content=equinor/flotilla&utm_campaign=Badge_Grade)

Flotilla is the main point of access for operators to interact with multiple robots in a facility. The application
consists of a [frontend](frontend) in React, a [backend](backend) in ASP.NET and a Mosquitto MQTT [Broker](broker).

## Deployments

We currently have 1 environment (Dev) deployed to Radix for demo purposes.

| Environment | Deployment                                                                                                                                              | Status                                                                                                     |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Dev         | [Frontend](https://frontend-flotilla-dev.playground.radix.equinor.com/)<br>[Backend](https://backend-flotilla-dev.playground.radix.equinor.com/swagger) | ![Dev](https://api.playground.radix.equinor.com/api/v1/applications/flotilla/environments/dev/buildstatus) |
|             |

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

## Database model and EF Core

Our database model is defined in the folder
[`/backend/api/Database/Models`](/backend/api/Database/Models) and we use
[Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) as an
object-relational mapper (O/RM). When making changes to the model, we also need
to create a new
[migration](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
and apply it to our databases.

### Installing EF Core

```bash
dotnet tool install --global dotnet-ef
```

### Creating a new migration

**NB: Make sure you have have fetched the newest code from main and that no-one else
is making migrations at the same time as you!**
After making changes to the model, run the following command from `/backend/api`:

```bash
dotnet ef migrations add {migration-name}
```

`add` will make changes to existing files and add 2 new files in
`backend/api/Migrations`, which all need to be checked in to git.

Note that the {migration-name} is just a descriptive name of your choosing.
Also note that `Database__ConnectionString` should be pointed at one of our
databases when running `add`. The reason for this is that the migration will be
created slightly different when based of the in-memory database. `add` will _not_
update or alter the connected database in any way.

If you for some reason are unhappy with your migration, you can delete it with

```bash
dotnet ef migrations remove
```

Once removed you can make new changes to the model
and then create a new migration with `add`.

### Applying the migrations to the dev- and test database

For the migration to take effect, we need to apply it to our databases. To get
an overview of the current migrations in a database, set the correct
`Database__ConnectionString` for that database and run:

```bash
dotnet ef migrations list
```

This will list all migrations that are applied to the database and the local
migrations that are yet to be applied. The latter are denoted with the text
(pending).

To apply the pending migrations to the database run:

```bash
dotnet ef database update
```

If everything runs smoothly the pending tag should be gone if you run `list`
once more.

### When to apply the migration to our databases

You can apply migrations to the dev database at any time to test that it
behaves as expected.

The prod and qa databases doesn't need to be updated manually, as all migrations are
applied to it automatically as part of the pipelines when pushed to qa and prod.

## Run with docker

Install docker using the [official documentation](https://docs.docker.com/engine/install/ubuntu/).

Install docker compose:

```
sudo apt update
sudo apt install docker-compose
```

Build the docker container:

```
docker-compose build
```

Setup a .env file in the backend directory with the following environment variables:

```
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_CLIENT_SECRET
```

Start Flotilla by running:

```
docker-compose up
```

or

```
docker-compose up --build
```

## Contributing

We welcome all kinds of contributions, including code, bug reports, issues, feature requests, and documentation. The preferred way of submitting a contribution is to either make an [issue](https://github.com/equinor/isar/issues) on github or by forking the project on github and making a pull requests.

We write our commit messages according to [this guide](https://cbea.ms/git-commit/).
