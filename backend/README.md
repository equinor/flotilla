# Flotilla backend

The backend of flotilla is created using ASP.NET.
Useful documentation of concepts and features in the .NET frameworks can be found
[here](https://docs.microsoft.com/en-us/dotnet/fundamentals/).

## Setup

To set up the backend on **Windows/Mac**, install visual studio and include the "ASP.NET and web development" workload during install.
If you already have visual studio installed, you can open the "Visual Studio Installer" and modify your install to add the workload.

To set up the backend on **Linux**, install .NET for linux
[here](https://docs.microsoft.com/en-us/dotnet/core/install/linux).

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
`appsetings.Development.json` file is for variables specific to the Dev environments, such as the client ID's for the
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

The staging and prod databases doesn't need to be updated manually, as all migrations are
applied to it automatically as part of the pipelines when pushed to staging and prod.

## Formatting

### CSharpier

In everyday development we use [CSharpier](https://csharpier.com/) to auto-format code on save. Installation procedure is described [here](https://csharpier.com/docs/About). No configuration should be required.

### Dotnet format

The formatting of the backend is defined in the [.editorconfig file](../.editorconfig).

We use [dotnet format](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
to format and verify code style in backend based on the
[C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

Dotnet format is included in the .NET6 SDK.

To check the formatting, run the following command in the backend folder:

```
cd backend
dotnet format --severity info --verbosity diagnostic --verify-no-changes --exclude ./api/migrations
```

dotnet format is used to detect naming conventions and other code-related issues. They can be fixed by

```
dotnet format --severity info
```

## Monitoring

We use [Azure Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)
to monitor the backend of our application.

We have one application insight instance for each environment.
The connection strings for the AI instances are stored in the keyvault.
