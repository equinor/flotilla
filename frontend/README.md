# Flotilla frontend

## Setup

The app uses TypeScript and React. For development, Node v17.x needs to be installed. Installation instructions can be found
[here](https://github.com/nodesource/distributions/blob/master/README.md).

The application reads environment variables from the `.env` file in the `frontend` folder.

### Automatic environment setup

See [Flotilla readme](../README.md#automatic-environment-setup)

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

This command runs the app in the development mode. Open [http://localhost:3001](http://localhost:3001) to view it in the browser.

The page will reload if you make edits. You will also be able to see any lint errors in the console.

## Run in Docker
To run the frontend in docker, run the following command in the root folder of flotilla:
```
docker-compose up --build frontend
```

## Authentication

Authentication is implemented for the frontend following the [official Microsoft tutorial on Oauth2 flow in React](https://docs.microsoft.com/en-us/azure/active-directory/develop/tutorial-v2-react).

## Automatically generated models

The typescript models have been automatically generated using an [openapi-to-typescript npm package](https://www.npmjs.com/package/openapi-typescript).
This can be updated by cloning the [flotilla-openapi](https://github.com/equinor/flotilla-openapi) repository and then running:

```bash
    npx openapi-typescript <path-to-flotilla-openapi>/openapi.yaml --output ./src/models/schema.ts
```

## Formatting

We use prettier for formatting.  
To test the formatting locally, run

```
npm run prettier_check
```

We recommend to install the [prettier extension for vs code](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)
and set the `format on save` option for vs code to true.  
You can do this by going to `File` -> `Preferences` -> `Settings` and then searching for "Format On Save" and tick the box.

## Config

The application reads custom environment variables from the `.env` file on startup. The files need to be prefixed `REACT_APP_` to be included in the application.  
These are parsed and defined in [config.ts](./src/config.ts).
