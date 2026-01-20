# Flotilla frontend

This document describes how to run the frontend. For information on best practices related to development, [see the best practices document](./best_practices.md).

## Setup

The app uses TypeScript and React. For development, Node v20.x needs to be installed. Installation instructions can be found
[here](https://github.com/nodesource/distributions/blob/master/README.md).

The application reads environment variables from the `.env` file in the `frontend` folder.

### Automatic environment setup

See [Flotilla readme](../README.md#automatic-environment-setup).

### Manual environment setup

As a starting point, make a copy of the `.env.example` file and rename it to `.env`.

## Install

To install the application, navigate to the frontend folder and run the following command:

```
npm ci
```

## Run

To start the app, run the following command in the root folder:

```
npm start
```

This command runs the app in development mode. Open [http://localhost:3001/robotics-frontend](http://localhost:3001/robotics-frontend) to view it in the browser.

The page will reload if you make edits. You will also be able to see any lint errors in the console.

## Run in Docker

To run the frontend in Docker, run the following command in the root folder of Flotilla:

```
docker compose up --build frontend
```

## Authentication

Authentication is implemented for the frontend following the [official Microsoft tutorial on OAuth2 flow in React](https://docs.microsoft.com/en-us/azure/active-directory/develop/tutorial-v2-react).

## Automatically generated models

The TypeScript models have been automatically generated using an [openapi-to-typescript npm package](https://www.npmjs.com/package/openapi-typescript).
This can be updated by cloning the [flotilla-openapi](https://github.com/equinor/flotilla-openapi) repository and then running:

```bash
npx openapi-typescript <path-to-flotilla-openapi>/openapi.yaml --output ./src/models/schema.ts
```

## Formatting

We use Prettier for formatting.
To test the formatting locally, run:

```
npm run prettier_check
```

We recommend installing the [Prettier extension for VS Code](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)
and setting the `format on save` option for VS Code to true.
You can do this by going to `File` -> `Preferences` -> `Settings` and then searching for "Format On Save" and ticking the box.

## Config

The application reads custom environment variables from the `.env` file on startup. The variables need to be prefixed with `VITE_` to be included in the application.
These are parsed and defined in [config.ts](./src/config.ts).

## Run locally with Staging and Prod Databases

To run locally towards the databases, follow the steps below:

1. Change `UseInMemoryDatabase` to `false` in `appsettings.Local` and set `ASPNETCORE_ENVIRONMENT` to the correct environment in `launchSettings.json`.
3. Update `VITE_BACKEND_API_SCOPE` in the `.env` file located in the `frontend` folder.
