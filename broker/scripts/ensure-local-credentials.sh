#!/usr/bin/env bash
# Ensures this machine has its own local broker credentials, and writes the
# broker/.env that docker compose feeds to the broker. The single entry point for
# local development: ../../setup.sh and ../../tilt/preflight.py both call it, so
# the interactive and the Tilt route always produce the same artefacts.
#
# Usage: ./ensure-local-credentials.sh [-f] [-q]
#
#   -f  Rotate: regenerate even when the current credentials are fine.
#   -q  Suppress the human-readable progress on stderr.
#
# Credentials are reused unless they are missing or the certificate is within
# RENEW_SECONDS of expiring. Rotating invalidates every password, and the clients
# hold copies of those, so it is not something to do on every run.
#
# Prints one word on stdout, `current` or `generated`, saying whether it had to
# mint a new set. Everything else goes to stderr, so a caller can branch on the
# outcome without parsing prose.

set -euo pipefail

FORCE=""
QUIET=""

while getopts ":fq" option; do
    case "${option}" in
        f) FORCE="true" ;;
        q) QUIET="true" ;;
        *)
            echo "Unknown option -${OPTARG}" >&2
            exit 1
            ;;
    esac
done

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
BROKER_DIR=$(cd -- "${SCRIPT_DIR}/.." && pwd)

CREDENTIALS_DIR="${BROKER_DIR}/.local-credentials"
BROKER_ENV="${BROKER_DIR}/.env"
GENERATOR="${SCRIPT_DIR}/generate-mqtt-credentials.sh"

# Every name a client may reach the broker by; the certificate is valid for these
# and nothing else. The stacks connect on localhost; `broker` and
# host.docker.internal cover connections from inside a container.
HOSTNAMES=(broker localhost host.docker.internal IP:127.0.0.1)

# Regenerate well before expiry, so rotating is never urgent. 30 days.
RENEW_SECONDS=$((30 * 24 * 60 * 60))

log() {
    [ -n "${QUIET}" ] || echo "$@" >&2
}

# Whether a full set exists and the certificate is not close to expiring.
credentials_are_current() {
    local name
    for name in server-cert.pem server-key.pem ca-cert.pem passwords mqtt-passwords.env; do
        [ -f "${CREDENTIALS_DIR}/${name}" ] || return 1
    done

    openssl x509 -in "${CREDENTIALS_DIR}/server-cert.pem" -noout \
        -checkend "${RENEW_SECONDS}" > /dev/null 2>&1
}

# The base64 body of a PEM, on one line, without the BEGIN/END markers.
pem_body() {
    grep -v -- '-----' "$1" | tr -d '\n'
}

# docker compose reads broker/.env through env_file, which cannot hold a
# multi-line value, so the PEM bodies go in on a single line. The broker
# reassembles them; see broker/entrypoint.sh.
write_broker_env() {
    {
        echo "TLS_SERVER_KEY='$(pem_body "${CREDENTIALS_DIR}/server-key.pem")'"
        echo "TLS_SERVER_CERT='$(pem_body "${CREDENTIALS_DIR}/server-cert.pem")'"
        echo "TLS_CA_CERT='$(pem_body "${CREDENTIALS_DIR}/ca-cert.pem")'"
        cat "${CREDENTIALS_DIR}/mqtt-passwords.env"
    } > "${BROKER_ENV}"
    chmod 0600 "${BROKER_ENV}"
}

if [ ! -f "${GENERATOR}" ]; then
    echo "ERROR: missing ${GENERATOR}." >&2
    exit 1
fi

outcome="current"

if [ -n "${FORCE}" ] || ! credentials_are_current; then
    # Local development gets its own throwaway credentials. Nothing here is
    # shared with a deployed environment, so a laptop cannot leak one.
    log "Generating local broker credentials in ${CREDENTIALS_DIR} ..."
    rm -rf "${CREDENTIALS_DIR}"
    "${GENERATOR}" -o "${CREDENTIALS_DIR}" "${HOSTNAMES[@]}" > /dev/null
    outcome="generated"
else
    log "Reusing the local broker credentials in ${CREDENTIALS_DIR}."
fi

# Written every time, so a deleted or corrupt .env heals without disturbing the
# passwords the clients already hold.
write_broker_env
log "Wrote ${BROKER_ENV}."

echo "${outcome}"
