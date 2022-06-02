# Flotilla backend

The backend of flotilla is created using ASP.NET.  
Useful documentation of concepts and features in the .NET frameworks can be found
[here](https://docs.microsoft.com/en-us/dotnet/fundamentals/).

## Setup

To set up the backend on **Windows/Mac**, install visual studio and include the "ASP.NET and web development" workload during install.  
If you already have visual studio installed, you can open the "Visual Studio Installer" and modify your install to add the workload.

To set up the backend on **Linux**, install .NET for linux
[here](https://docs.microsoft.com/en-us/dotnet/core/install/linux).

For the configuration to be able to read secrets from the keyvault, you will need to have the client secret stored locally
in your secret manager as described in the [Configuration Section](#Configuration).   

For the MQTT client to function, the application expects a config variable named `mqtt-broker-password`, containing the password for the mqtt broker. This must either be stored in a connected keyvault or in the ASP.NET secret manager. 

## Run

To build and run the app, run the following command in the backend folder:

```
dotnet run --project api
```

To change the ports of the application and various other launch settings (such as the Environment), this can be modified in
[launchSettings.json](api/Properties/launchSettings.json).  
Read more about the `launchSettings.json` file
[here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-6.0&preserve-view=true&viewFallbackFrom=aspnetcore-2.2#lsj)

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
The client secret should be in the following format: 
```
  "AzureAd": {
    "ClientSecret": "SECRET"
  }
```


Any local secrets used for configuration should be added in the
[ASP.NET Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=linux#secret-manager).

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
dotnet format --severity info --verbosity diagnostic --verify-no-changes
```

dotnet format is used to detect naming conventions and other code-related issues. They can be fixed by

```
dotnet format --severity info
```


