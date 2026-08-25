#!/usr/bin/env bash
# Generates MQTT credentials for one environment: a CA, a server certificate for
# the broker, and a password per ACL user. Store the secrets in that
# environment's key vault. See broker/README.md.
#
# Usage: ./generate-mqtt-credentials.sh <broker-hostname> [output-directory]

set -euo pipefail

HOSTNAME=${1:-}
OUTPUT_DIR=${2:-./mqtt-credentials}
VALIDITY_DAYS=${VALIDITY_DAYS:-825}

if [ -z "${HOSTNAME}" ]; then
    echo "Usage: $0 <broker-hostname> [output-directory]" >&2
    echo "  <broker-hostname> must match the host clients connect to, e.g. 'broker'." >&2
    exit 1
fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
ACL_FILE="${SCRIPT_DIR}/../mosquitto/config/access_control"

mkdir -p "${OUTPUT_DIR}"
chmod 0700 "${OUTPUT_DIR}"

echo "Generating CA..."
openssl req -x509 -newkey rsa:4096 -sha256 -days "${VALIDITY_DAYS}" -nodes \
    -keyout "${OUTPUT_DIR}/ca-key.pem" -out "${OUTPUT_DIR}/ca-cert.pem" \
    -subj "/CN=Flotilla MQTT CA ${HOSTNAME}" 2>/dev/null

echo "Generating server certificate for ${HOSTNAME}..."
openssl req -newkey rsa:4096 -sha256 -nodes \
    -keyout "${OUTPUT_DIR}/server-key.pem" -out "${OUTPUT_DIR}/server.csr" \
    -subj "/CN=${HOSTNAME}" 2>/dev/null

openssl x509 -req -in "${OUTPUT_DIR}/server.csr" -sha256 -days "${VALIDITY_DAYS}" \
    -CA "${OUTPUT_DIR}/ca-cert.pem" -CAkey "${OUTPUT_DIR}/ca-key.pem" -CAcreateserial \
    -out "${OUTPUT_DIR}/server-cert.pem" \
    -extfile <(printf "subjectAltName=DNS:%s\nbasicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n" "${HOSTNAME}")

rm -f "${OUTPUT_DIR}/server.csr" "${OUTPUT_DIR}/ca-cert.srl"

echo "Generating passwords for the users in access_control..."
: > "${OUTPUT_DIR}/passwords"
chmod 0600 "${OUTPUT_DIR}/passwords"

MQTT_PASSWORDS=""
while read -r _ user; do
    password=$(openssl rand -base64 32 | tr -d '/+=' | head -c 32)
    echo "${user}=${password}" >> "${OUTPUT_DIR}/passwords"
    MQTT_PASSWORDS="${MQTT_PASSWORDS}${MQTT_PASSWORDS:+,}${user}:${password}"
done < <(grep '^user ' "${ACL_FILE}")

echo "MQTT_PASSWORDS=${MQTT_PASSWORDS}" > "${OUTPUT_DIR}/mqtt-passwords.env"
chmod 0600 "${OUTPUT_DIR}/mqtt-passwords.env"

cat <<EOF

Written to ${OUTPUT_DIR}:

  ca-cert.pem          Not secret. Clients verify the broker against this.
                       ISAR reads it as ISAR_MQTT_CA_CERT.
  ca-key.pem           SECRET. Only needed to reissue server-cert.pem.
  server-cert.pem      Not secret. Broker reads it as TLS_SERVER_CERT.
  server-key.pem       SECRET. Broker reads it as TLS_SERVER_KEY.
  passwords            SECRET. One user=password line per ACL user.
  mqtt-passwords.env   SECRET. Broker reads it as MQTT_PASSWORDS.

Store the secrets in this environment's key vault. Do not commit them.
EOF
