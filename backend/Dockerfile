# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:35d40304542c8689331f8cab17c65926cdf48fe711e289321d71924b230a7d29 AS build
WORKDIR /source

# copy everything
COPY . .

WORKDIR /source/api
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:2d584d8147faddb0d678c5748d47953e5b8e18621ed4fb7049a91381d9d7746f
WORKDIR /app
COPY --from=build /app ./
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    apt-utils \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8000

# Runtime user change to non-root for added security
RUN useradd -ms /bin/bash --uid 1001 isar
RUN chown -R 1001 /app
RUN chmod 755 /app
USER 1001

ENTRYPOINT ["dotnet", "api.dll", "--urls=http://0.0.0.0:8000"]
