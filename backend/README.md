# Flotilla backend

The backend of flotilla is created using ASP.NET.
Useful documentation of concepts and features in the .NET frameworks can be found
[here](https://docs.microsoft.com/en-us/dotnet/fundamentals/).

- [Flotilla backend](#flotilla-backend)
  - [Setup](#setup)
    - [Automatic environment setup](#automatic-environment-setup)
    - [Manual environment setup](#manual-environment-setup)
  - [Run](#run)
    - [Run in Docker](#run-in-docker)
  - [Test](#test)
  - [Components](#components)
    - [MQTT Client](#mqtt-client)
  - [Configuration](#configuration)
  - [Database model and EF Core](#database-model-and-ef-core)
    - [Installing EF Core](#installing-ef-core)
    - [Adding a new migration](#adding-a-new-migration)
    - [Notes](#notes)
    - [Applying the migrations to the dev database](#applying-the-migrations-to-the-dev-database)
    - [Applying migrations to staging and production databases](#applying-migrations-to-staging-and-production-databases)
  - [Formatting](#formatting)
    - [CSharpier](#csharpier)
    - [Dotnet format](#dotnet-format)
  - [Monitoring](#monitoring)
  - [Authorization](#authorization)

## Setup

To set up the backend on **Windows/Mac**, install visual studio and include the "ASP.NET and web development" workload during install.
If you already have visual studio installed, you can open the "Visual Studio Installer" and modify your install to add the workload.

To set up the backend on **Linux**, install .NET for linux
[here](https://docs.microsoft.com/en-us/dotnet/core/install/linux).
You need to also install the dev certificate for local .NET development on linux.
Follow
[this guide](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-7.0&tabs=visual-studio%2Clinux-ubuntu#trust-https-certificate-on-linux),
for each of the browser(s) you wish to trust it in.
**NB:** You probably need to run the commands with `sudo` prefixed to have permission to change them.

For the configuration to be able to read secrets from the keyvault, you will need to have the client secret stored locally in your secret manager.

For the MQTT client to function, the application expects a config variable in the MQTT section called `Password`, containing the password for the mqtt broker.
This must either be stored in a connected keyvault as "Mqtt--Password" or in the ASP.NET secret manager
as described in the [configuration section](#Configuration).

### Automatic environment setup

See [Flotilla readme](../README.md#automatic-environment-setup)

### Manual environment setup

Add the client secret as described in the [Configuration Section](#Configuration).

## Run

To build and run the app, run the following command in the backend folder:

```
dotnet run --project api
```

To change the ports of the application and various other launch settings (such as the Environment), this can be modified in
[launchSettings.json](api/Properties/launchSettings.json).
Read more about the `launchSettings.json` file
[here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-6.0&preserve-view=true&viewFallbackFrom=aspnetcore-2.2#lsj)

### Run in Docker

For the backend to work when dockerized, you need to have the client secret exposed as
an environment variable named `FLOTILLA_CLIENT_SECRET`.
The best way to do this is to store it in an `.env` file in the root of the flotilla repository.
See [Using the “--env-file” option](https://docs.docker.com/compose/environment-variables/#using-the---env-file--option) for more information.

To run the backend in docker, run the following command in the root folder of flotilla:

```
docker compose up --build backend
```

## Test

To unit test the backend, run the following command in the backend folder:

```
dotnet test
```

## Components

### MQTT Client

The MQTT client is implemented in [MqttService.cs](api/MQTT/MqttService.cs)
and runs as an ASP.NET
[BackgroundService](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-6.0&tabs=visual-studio#backgroundservice-base-class).
Each MQTT message has its own class representation, and is linked to its respective topic pattern in [MqttTopics.cs](api/MQTT/MqttTopics.cs).
To match incoming topic messages against the topic patterns we use helper functions to convert from the
[MQTT wildcards](https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901242)
to regEx wildcards for the dictionnary lookup.

Each topic then has it's respective [event](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/events/)
which is triggered whenever a new message arrives in that topic.
The list of topics being subscribe to is defined as an array in
[appsettings.Development.json](api/appsettings.Development.json).

An example of the subscriber pattern for an MQTT event is implemented in
[MqttEvenHandler.cs](api/EventHandlers/MqttEventHandler.cs).

## Configuration

The project has two [appsettings](https://docs.microsoft.com/en-us/iis-administration/configuration/appsettings.json)
files.
The base `appsettings.json` file is for common variables across all environments, while the
`appsettings.Development.json` file is for variables specific to the Dev environments, such as the client ID's for the
various app registrations used in development.

The configuration will also read from a configured azure keyvault, which can then be accessed the same way as any other config variables.
For this to work you will need to have the client secret stored locally in the secret manager as described below.
The client secret (and mqtt password if not connected to keyvault) should be in the following format:

```
  "AzureAd": {
    "ClientSecret": "SECRET"
  },
  "Mqtt": {
    "Password": "PASSWORD"
  }
```

Any local secrets used for configuration should be added in the
[ASP.NET Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=linux#secret-manager).

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

### Adding a new migration

**NB: Make sure you have have fetched the newest code from main and that no-one else
is making migrations at the same time as you!**

1. Set the environment variable `ASPNETCORE_ENVIRONMENT` to `Development`:

   ```bash
    export ASPNETCORE_ENVIRONMENT=Development
   ```

2. Run the following command from `/backend/api`:
   ```bash
     dotnet ef migrations add AddTableNamePropertyName
   ```
   `add` will make changes to existing files and add 2 new files in
   `backend/api/Migrations`, which all need to be checked in to git.

### Notes

- The `your-migration-name-here` is basically a database commit message.
- `Database__ConnectionString` will be fetched from the keyvault when running the `add` command.
- `add` will _not_ update or alter the connected database in any way, but will add a
  description of the changes that will be applied later
- If you for some reason are unhappy with your migration, you can delete it with:
  ```bash
  dotnet ef migrations remove
  ```
  Once removed you can make new changes to the model
  and then create a new migration with `add`.

### Applying the migrations to the dev database

Updates to the database structure (applying migrations) are done in Github Actions.

When a pull request contains changes in the `backend/api/Database/Migrations` folder,
[a workflow](https://github.com/equinor/flotilla/blob/main/.github/workflows/notifyMigrationChanges.yml)
is triggered to notify that the pull request has database changes.

After the pull request is approved, a user can then trigger the database changes by commenting
`/UpdateDatabase` on the pull request.

This will trigger
[another workflow](https://github.com/equinor/flotilla/blob/main/.github/workflows/updateDatabase.yml)
which updates the database by apploying the new migrations.

By doing migrations this way, we ensure that the commands themselves are scripted, and that the database
changes become part of the review process of a pull request.

### Applying migrations to staging and production databases

This is done automatically as part of the promotion workflows
([promoteToProduction](https://github.com/equinor/flotilla/blob/main/.github/workflows/promoteToProduction.yml)
and [promoteToStaging](https://github.com/equinor/flotilla/blob/main/.github/workflows/promoteToStaging.yml)).

## Database setup

If reseting database, but still using postgresql (removing old migrations and adding them manually again), you need to manually add some migrations commands for the timeseries db.

Add the following to the **top of the first migration's** `Up()` method:

```
            // MANUALLY ADDED:
            // Adding timescale extension to the database
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");
            //
```

**For each of the timeseries tables**, add the following to the **bottom of the last migration's** `Up()` method:

```
            migrationBuilder.Sql(
                "SELECT create_hypertable( '\"TIME-TABLE-NAME\"', 'Time');\n"
            );
```

Example:

```
            migrationBuilder.Sql(
                "SELECT create_hypertable( '\"RobotBatteryTimeseries\"', 'Time');\n"
            );
```

## Database backup and cloning

You can use pg_dump to extract a PostgreSQL database into an sql file and psql to import the data into the target database from that file. Have the server running on pgAdmin and then execute the following commands.

Extract the entire database:

```
pg_dump -U Username -d postgres -h host_name_or_adress -p port -f ouput_file_name.slq
```

Extract specific tables:

```
pg_dump -U Username -d postgres -h host_name_or_adress -p port -t '"table_name"' -t '"second_table_name"' -f input_file_name.slq
```

Upload file information to new database:

```
psql U Username -d postgres -h host_name_or_adress -p port -f ouput_file_name.slq
```

## Formatting

### CSharpier

In everyday development we use [CSharpier](https://csharpier.com/) to auto-format code on save. Installation procedure is described [here](https://csharpier.com/docs/About). No configuration should be required.

### Dotnet format

The formatting of the backend is defined in the [.editorconfig file](../.editorconfig).

We use [dotnet format](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
to format and verify code style in backend based on the
[C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

Dotnet format is included in the .NET SDK.

To check the formatting, run the following command in the backend folder:

```
cd backend
dotnet format --severity info --verbosity diagnostic --verify-no-changes --exclude ./api/migrations
```

dotnet format is used to detect naming conventions and other code-related issues. They can be fixed by

```
dotnet format --severity info
```

## SignalR

We use SignalR to asynchronously send event updates to the frontend. Currently we only support sending
events and not receiving them, and all transmissions are sent using the SignalRService class. When
doing so it is important to make sure that the event name provided corresponds with the name expected
in the frontend.

It is also crucial that we do not await sending signalR messages in our code. Instead we ignore the
await warning. In the current version of the SignalR library, sending a message in an
asynchronous thread may cause the thread to silently exit without returning an exception, which is
avoided by letting the SignalR code run asynchronously after the current thread has executed.

## Monitoring

We use [Azure Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)
to monitor the backend of our application.

We have one application insight instance for each environment.
The connection strings for the AI instances are stored in the keyvault.

## Custom Mission Loaders

You can create your own mission loader to fetch missions from some external system. The custom mission loader needs to fulfill the [IMissionLoader](api/Services/MissionLoaders/MissionLoaderInterface.cs) interface. If you mission loader is an external API you might need to add it as a downstreamapi in [Program.cs](api/Program.cs)

## Authorization

We use role based access control (RBAC) for authorization.

The access matrix looks like this:

|                            | **Read Only** | **User** | **Admin** |
| -------------------------- | ------------- | -------- | --------- |
| Area                       | Read          | Read     | CRUD      |
| InspectionArea             | Read          | Read     | CRUD      |
| Plant                      | Read          | Read     | CRUD      |
| Installation               | Read          | Read     | CRUD      |
| MissionLoader              | Read          | Read     | CRUD      |
| Missions                   | Read          | Read     | CRUD      |
| Robots                     | Read          | Read     | CRUD      |
| Robot Models               | Read          | Read     | CRUD      |
| Start Missions Directly    | ❌            | ❌       | ✔️        |
| Stop/Pause/Resume Missions | ❌            | ✔️       | ✔️        |
| Localize Robots            | ❌            | ✔️       | ✔️        |
| Send Robot to Dock         | ❌            | ✔️       | ✔️        |
