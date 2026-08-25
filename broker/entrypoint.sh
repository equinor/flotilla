#!/bin/sh
set -eu

RUNTIME_DIR=mosquitto/config/runtime
DEFAULT_CERTS_DIR=mosquitto/config/certs

# Certificates and the password file are assembled into RUNTIME_DIR on startup.
# The environment wins; anything absent falls back to the committed defaults.
# See scripts/generate-mqtt-credentials.sh.

write_pem() {
    # Accepts either a full PEM or the bare base64 body the key vault holds.
    value=$1
    label=$2
    destination=$3

    if echo "${value}" | grep -q -- "-----BEGIN"; then
        echo "${value}" > "${destination}"
    else
        echo "-----BEGIN ${label}-----" > "${destination}"
        echo "${value}" | tr -d "'" >> "${destination}"
        echo "-----END ${label}-----" >> "${destination}"
    fi

    chmod 0600 "${destination}"
}

write_password_file() {
    # MQTT_PASSWORDS is user:password pairs, and must cover every user in
    # mosquitto/config/access_control.
    destination=$1

    rm -f "${destination}"
    touch "${destination}"
    chmod 0600 "${destination}"

    echo "${MQTT_PASSWORDS}" | tr ',' '\n' | while read -r pair; do
        [ -z "${pair}" ] && continue
        user=$(echo "${pair}" | cut -d: -f1)
        password=$(echo "${pair}" | cut -d: -f2-)
        if [ -z "${user}" ] || [ -z "${password}" ]; then
            echo "ERROR: MQTT_PASSWORDS entry is not of the form user:password" >&2
            exit 1
        fi
        mosquitto_passwd -b "${destination}" "${user}" "${password}"
    done
}

mkdir -p "${RUNTIME_DIR}"

if [ -n "${MQTT_PASSWORDS:-}" ]; then
    write_password_file "${RUNTIME_DIR}/passwd_file"
else
    echo "WARNING: MQTT_PASSWORDS is not set — falling back to the password file committed to the image."
    cp mosquitto/config/passwd_file "${RUNTIME_DIR}/passwd_file"
    chmod 0600 "${RUNTIME_DIR}/passwd_file"
fi

if [ "${MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS:-}" = "true" ]; then
    echo "WARNING: MQTT TLS disabled — insecure connections allowed. Do NOT use in production."
    exec mosquitto -p 1883 -c mosquitto/config/mosquitto-notls.conf
fi

if [ -n "${TLS_SERVER_KEY:-}" ]; then
    write_pem "${TLS_SERVER_KEY}" "PRIVATE KEY" "${RUNTIME_DIR}/server-key.pem"
else
    echo "ERROR: TLS_SERVER_KEY is required unless MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS=true." >&2
    exit 1
fi

if [ -n "${TLS_SERVER_CERT:-}" ]; then
    write_pem "${TLS_SERVER_CERT}" "CERTIFICATE" "${RUNTIME_DIR}/server-cert.pem"
else
    cp "${DEFAULT_CERTS_DIR}/server-cert.pem" "${RUNTIME_DIR}/server-cert.pem"
fi

if [ -n "${TLS_CA_CERT:-}" ]; then
    write_pem "${TLS_CA_CERT}" "CERTIFICATE" "${RUNTIME_DIR}/ca-cert.pem"
else
    cp "${DEFAULT_CERTS_DIR}/ca-cert.pem" "${RUNTIME_DIR}/ca-cert.pem"
fi

exec mosquitto -p 1883 -c mosquitto/config/mosquitto.conf
