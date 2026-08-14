# Flotilla frontend

The Flotilla frontend is a React application written in TypeScript and built with Vite.

For development conventions, folder structure, and formatting, see [best_practices.md](./best_practices.md).

## Prerequisites

- [Node.js 24.x](https://github.com/nodesource/distributions)
- [pnpm](https://pnpm.io/installation)

## Configuration

The application reads environment variables from `frontend/.env`. Create it by running [`setup.sh`](../setup.sh) from the repository root, or by copying the template manually:

```bash
cp .env.example .env
```

Only variables prefixed with `VITE_` are included in the application. They are parsed and defined in [config.ts](./src/config.ts).

## Install

From the `frontend` folder:

```bash
pnpm install --frozen-lockfile
```

## Run

```bash
pnpm dev      # or: make run
```

The app is served at <http://localhost:3001> in development mode. The page reloads when you make edits, and lint errors appear in the console.

To run the frontend in Docker, run the following from the repository root:

```bash
docker compose up --build frontend
```

## Run against the Staging or Production backend

1. Update `VITE_BACKEND_API_SCOPE` in `frontend/.env` to the scope of the environment you want to target.
2. Point the backend at that environment as well — see the [backend README](../backend/README.md#connecting-to-the-development-database).
