# Flotilla backend

## Run

To build and run the app, run the following command in the backend folder:

```
dotnet run
```

## Test

To unit test the backend, run the following command in the backend folder:

```
dotnet test
```

## Formatting

The formatting of the backend is defined in the [.editorconfig file](../editorconfig).  

We use [dotnet format](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
to format and verify code style in backend based on the 
[C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).  

Dotnet format is included in the .NET6 SDK.

To check the formating, run 
```
cd backend
dotnet format --severity info --verbosity diagnostic --verify-no-changes
```
and to fix the formatting run
```
cd backend
dotnet format --severity info
```
  
