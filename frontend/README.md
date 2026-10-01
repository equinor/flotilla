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

## Tests

Keep frontend tests in `frontend/tests/`, with subfolders matching the source area
(for example, `tests/contexts/` and `tests/hooks/`), rather than colocating them in `src/`.
With `.env` configured as described above, run the suite from the frontend folder with
`pnpm test --run`. The App test imports application configuration and requires those variables.

## Livestream recovery

Mission and robot camera views acquire media through
[MediaStreamManager.ts](./src/contexts/MediaStreamManager.ts). Starting a new viewing session or
manager-level retry fetches fresh media configuration from the backend: this also activates the robot's
publisher, which connecting with a cached LiveKit token alone does not do. The frontend no longer
reads or writes the legacy `mediaConfigs` credential cache.

Both transports use the code defaults and backoff helper in
[MediaStreamRecoveryPolicy.ts](./src/contexts/MediaStreamRecoveryPolicy.ts):

- `recoveryTimeoutMs` (**30 seconds**) bounds each connection/recovery wait. Repeated interruption
  events preserve the existing deadline rather than extending it.
- `stabilityResetMs` (**30 seconds**) is the uninterrupted healthy-state window required to reset the
  attempt counter to zero. An interruption cancels the window, so short-lived recoveries do not
  replenish the budget. Health is inferred from transport events, not measured frame progression.
- `maxAttempts` (**3**) includes the initial connection attempt, allowing two startup retries. After
  stability resets the counter, a later outage gets three fresh attempts. Manual **Retry** also resets
  the counter.
- `retryBaseDelayMs` (**2 seconds**) feeds exponential backoff through
  `calculateMediaStreamRetryDelayMs`. Startup retries wait **2, then 4 seconds**; after a stability
  reset, the waits before three fresh attempts are **2, 2, then 4 seconds**.

LiveKit views share one room per robot. Activation, room connection, and waiting for video share an
attempt deadline. Losing the last camera or a stalled native reconnect starts a recovery deadline;
a disconnected room starts recovery after backoff. Partial camera loss does not interrupt remaining
cameras. Video-track subscription and room connection events determine the healthy state.

For OvenMediaEngine (OME), [OmeStreamManager.ts](./src/contexts/OmeStreamManager.ts) maintains a
separate deadline and retry budget per camera. Once the initial configuration arrives, each camera
starts its own deadline; the timeout is therefore not an overall end-to-end limit for opening an OME
view. Each camera retry fetches fresh configuration and selects that camera's URL. Player `playing`
events establish health, while stall/loading/pause/idle events interrupt the stability window.
An exhausted camera displays **Stream unavailable** with its own **Retry** action while other cameras
continue playing. LiveKit exhaustion displays the same status and action for the robot connection.

The last camera viewer leaving disposes the robot's manager state and cancels recovery. LiveKit rooms
are disconnected, and OME views remove their players and cancel pending probes on unmount.
Re-rendering a page or receiving mission updates does not restart activation or reset the retry budget.

Livestream regression tests live in `tests/components/Contexts/MediaStreamContext.test.tsx`.
Run them with `pnpm test --run tests/components/Contexts/MediaStreamContext.test.tsx`.

## Run against the Staging or Production backend

1. Update `VITE_BACKEND_API_SCOPE` in `frontend/.env` to the scope of the environment you want to target.
2. Point the backend at that environment as well — see the [backend README](../backend/README.md#connecting-to-the-development-database).
