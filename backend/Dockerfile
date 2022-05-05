# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# copy everything
COPY . .

WORKDIR /source/api
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app ./

EXPOSE 8000

# Runtime user change to non-root for added security
USER 1001

ENTRYPOINT ["dotnet", "api.dll", "--urls=http://0.0.0.0:8000"]
