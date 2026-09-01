#!/bin/sh
set -eu

RUNTIME_DIR=mosquitto/config/runtime
DEFAULT_CERTS_DIR=mosquitto/config/certs
ACL_FILE=mosquitto/config/access_control
DEFAULT_PASSWD_FILE=mosquitto/config/passwd_file

# Certificates and the password file are assembled into RUNTIME_DIR on startup.
# The environment wins; anything absent falls back to the defaults committed to
# this image, which are shared by every environment and therefore not a secret.
# Set MQTT_REQUIRE_ENV_CREDENTIALS=true to refuse to start on any such fallback;
# every deployed environment should do so. See scripts/generate-mqtt-credentials.sh.

fallback_used=""

use_fallback() {
    # Records that a committed default was used, and says so. Silence here is how
    # an environment ends up believing it has rotated something it has not.
    what=$1
    variable=$2

    echo "WARNING: ${variable} is not set — using the ${what} committed to this image."
    fallback_used="${fallback_used}${fallback_used:+, }${variable}"
}

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

acl_users() {
    sed -n 's/^user  *\([^ ]*\).*/\1/p' "${ACL_FILE}"
}

write_password_file() {
    # MQTT_PASSWORDS is comma separated user:password pairs. A password may
    # contain a colon but not a comma, which is the pair separator.
    destination=$1

    rm -f "${destination}"
    touch "${destination}"
    chmod 0600 "${destination}"

    # A pipeline would put the loop in a subshell, where a failure cannot stop
    # the rest of this script. Iterate over a temporary file instead.
    pairs=$(mktemp)
    echo "${MQTT_PASSWORDS}" | tr ',' '\n' > "${pairs}"

    while read -r pair; do
        [ -n "${pair}" ] || continue
        user=$(echo "${pair}" | cut -d: -f1)
        password=$(echo "${pair}" | cut -d: -f2-)
        if [ -z "${user}" ] || [ "${user}" = "${pair}" ] || [ -z "${password}" ]; then
            echo "ERROR: MQTT_PASSWORDS entry is not of the form user:password." >&2
            rm -f "${pairs}"
            exit 1
        fi
        mosquitto_passwd -b "${destination}" "${user}" "${password}"
    done < "${pairs}"

    rm -f "${pairs}"
}

assert_every_acl_user_has_a_password() {
    # A user in access_control without a password cannot authenticate at all, and
    # mosquitto will not say why. Fail here instead, where the cause is obvious.
    passwd_file=$1
    missing=""

    for user in $(acl_users); do
        grep -q "^${user}:" "${passwd_file}" || missing="${missing}${missing:+, }${user}"
    done

    if [ -n "${missing}" ]; then
        echo "ERROR: no password for the following users in ${ACL_FILE}: ${missing}." >&2
        echo "       MQTT_PASSWORDS must cover every user in that file." >&2
        exit 1
    fi
}

mkdir -p "${RUNTIME_DIR}"

if [ -n "${MQTT_PASSWORDS:-}" ]; then
    echo "Using the passwords from MQTT_PASSWORDS."
    write_password_file "${RUNTIME_DIR}/passwd_file"
else
    use_fallback "password file" MQTT_PASSWORDS
    cp "${DEFAULT_PASSWD_FILE}" "${RUNTIME_DIR}/passwd_file"
    chmod 0600 "${RUNTIME_DIR}/passwd_file"
fi

assert_every_acl_user_has_a_password "${RUNTIME_DIR}/passwd_file"

if [ "${MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS:-}" = "true" ]; then
    if [ "${MQTT_REQUIRE_ENV_CREDENTIALS:-}" = "true" ]; then
        echo "ERROR: MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS and MQTT_REQUIRE_ENV_CREDENTIALS" >&2
        echo "       contradict each other. Disabling TLS is for local development only." >&2
        exit 1
    fi
    echo "WARNING: MQTT TLS disabled — insecure connections allowed. Do NOT use in production."
    exec mosquitto -p 1883 -c mosquitto/config/mosquitto-notls.conf
fi

if [ -n "${TLS_SERVER_KEY:-}" ]; then
    echo "Using the server key from TLS_SERVER_KEY."
    write_pem "${TLS_SERVER_KEY}" "PRIVATE KEY" "${RUNTIME_DIR}/server-key.pem"
else
    echo "ERROR: TLS_SERVER_KEY is required unless MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS=true." >&2
    exit 1
fi

if [ -n "${TLS_SERVER_CERT:-}" ]; then
    echo "Using the server certificate from TLS_SERVER_CERT."
    write_pem "${TLS_SERVER_CERT}" "CERTIFICATE" "${RUNTIME_DIR}/server-cert.pem"
else
    use_fallback "server certificate" TLS_SERVER_CERT
    cp "${DEFAULT_CERTS_DIR}/server-cert.pem" "${RUNTIME_DIR}/server-cert.pem"
fi

if [ -n "${TLS_CA_CERT:-}" ]; then
    echo "Using the CA certificate from TLS_CA_CERT."
    write_pem "${TLS_CA_CERT}" "CERTIFICATE" "${RUNTIME_DIR}/ca-cert.pem"
else
    use_fallback "CA certificate" TLS_CA_CERT
    cp "${DEFAULT_CERTS_DIR}/ca-cert.pem" "${RUNTIME_DIR}/ca-cert.pem"
fi

if [ -n "${fallback_used}" ] && [ "${MQTT_REQUIRE_ENV_CREDENTIALS:-}" = "true" ]; then
    echo "ERROR: MQTT_REQUIRE_ENV_CREDENTIALS is set, but these fell back to the" >&2
    echo "       credentials committed to this image: ${fallback_used}." >&2
    exit 1
fi

exec mosquitto -p 1883 -c mosquitto/config/mosquitto.conf
