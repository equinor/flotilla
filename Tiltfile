# Flotilla standalone local stack.
#
# Spins up a self-contained Flotilla development environment:
#   - MQTT broker (Docker container, plain TCP -- TLS disabled for local dev)
#   - PostgreSQL (Docker container)
#   - Flotilla backend  (local process, dotnet watch)
#   - Flotilla frontend (local process, pnpm/vite)
#
# Secrets are fetched from Azure Key Vault at startup (requires: az login).
# See tilt/kv-secrets-ref.yaml for the secret-to-vault mapping.
#
# Usage: make run   (or: tilt up)
#
# To drive a mission, run an ISAR robot on the host pointed at this stack's
# broker (localhost:1883) and backend (localhost:8000).

update_settings(max_parallel_updates=6)

load("./tilt/flotilla.tilt", "flotilla_stack", "FLOTILLA_ROOT")

# Resolve build/volume paths in the compose files (see their headers).
os.putenv("FLOTILLA_ROOT", FLOTILLA_ROOT)

# ---------------------------------------------------------------------------
# Docker Compose: MQTT broker + PostgreSQL
# ---------------------------------------------------------------------------
docker_compose(
    [
        "./tilt/docker-compose.broker.yml",
        "./tilt/docker-compose.postgres.yml",
    ],
    project_name="flotilla",
)

dc_resource("mqtt-broker", labels=["infrastructure"])
dc_resource("postgres", labels=["infrastructure"])

# Wipe & repopulate the database on every `tilt up`. The postgres data lives in
# a named volume that survives restarts, so without this the DB would carry over
# stale state. This drops and recreates `flotilla`; the EF-migration step in
# flotilla-backend then rebuilds the schema and the seed data repopulates.
# WITH (FORCE) (Postgres 13+) terminates lingering connections so the drop can't
# hang. Each statement is a separate `-c` because DROP/CREATE DATABASE cannot run
# inside a transaction block.
local_resource(
    "postgres-reset",
    cmd='docker exec flotilla-postgres psql -U postgres -d postgres -v ON_ERROR_STOP=1' +
        ' -c "DROP DATABASE IF EXISTS flotilla WITH (FORCE)"' +
        ' -c "CREATE DATABASE flotilla"',
    resource_deps=["postgres"],
    labels=["infrastructure"],
)

# ---------------------------------------------------------------------------
# Flotilla backend + frontend
# ---------------------------------------------------------------------------
flotilla_stack(
    pg_conn="Host=localhost;Port=5432;Database=flotilla;Username=postgres;Password=postgres",
    mqtt_host="localhost",
    mqtt_port=1883,
    resource_deps=["mqtt-broker", "postgres-reset"],
)
