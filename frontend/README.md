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

## Livestream recovery

Mission and robot camera views share a LiveKit connection per robot while mounted. Starting a new
viewing session or retry always fetches fresh media configuration from the backend: this also activates
the robot's publisher, which connecting with a cached LiveKit token alone does not do. The frontend
no longer reads or writes the legacy `mediaConfigs` credential cache.

Activation, room connection, and waiting for video have a bounded deadline. Failed attempts retry
with exponential backoff up to an attempt limit, then display **Stream unavailable** with a manual
**Retry** action. Losing the last camera or a stalled native reconnect also has a recovery deadline;
a disconnected room starts recovery after backoff. Partial camera loss does not interrupt remaining
cameras. Sustained video replenishes the retry budget for a later independent outage; short-lived
tracks do not reset it.

The recovery policy is defined by `ATTEMPT_TIMEOUT_MS`, `MAX_ATTEMPTS`, `RETRY_DELAY_MS`, and
`STABLE_VIDEO_MS` in [MediaStreamManager.ts](./src/components/Contexts/MediaStreamManager.ts).

The last camera viewer leaving disconnects its room and cancels recovery. Re-rendering a page or
receiving mission updates does not restart activation or reset the retry budget.

Livestream regression tests live in `tests/components/Contexts/MediaStreamContext.test.tsx`.
Run them with `pnpm test --run tests/components/Contexts/MediaStreamContext.test.tsx`.

## Run against the Staging or Production backend

1. Update `VITE_BACKEND_API_SCOPE` in `frontend/.env` to the scope of the environment you want to target.
2. Point the backend at that environment as well — see the [backend README](../backend/README.md#connecting-to-the-development-database).
