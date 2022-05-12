# Flotilla backend

The backend of flotilla is created using ASP.NET.  
Useful documentation of concepts and features in the .NET frameworks can be found
[here](https://docs.microsoft.com/en-us/dotnet/fundamentals/).

## Setup

To set up the backend on **Windows/Mac**, install visual studio and include the "ASP.NET and web development" workload during install.  
If you already have visual studio installed, you can open the "Visual Studio Installer" and modify your install to add the workload.

To set up the backend on **Linux**, install .NET for linux
[here](https://docs.microsoft.com/en-us/dotnet/core/install/linux).

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

## Formatting

### CSharpier

In everyday development we use [CSharpier](https://csharpier.com/) to auto-format code on save. Installation procedure is described [here](https://csharpier.com/docs/About). No configuration should be required.

### Dotnet format

The formatting of the backend is defined in the [.editorconfig file](../editorconfig).

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

## Configuration

The project has two [appsettings](https://docs.microsoft.com/en-us/iis-administration/configuration/appsettings.json)
files.  
The base `appsettings.json` file is for common variables across all environments, while the
`appsetings.Development.json` file is for variables specific to the Dev environments, such as the client ID's for the
various app registrations used in development.

Any secrets used for configuration should be added in the
[ASP.NET Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=linux#secret-manager).
