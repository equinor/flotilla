#!/usr/bin/env bash
# Generates MQTT credentials for one environment: a CA, a server certificate for
# the broker, and a password per ACL user. Store the secrets in that
# environment's key vault. See broker/README.md.
#
# Usage: ./generate-mqtt-credentials.sh [-o OUTPUT_DIR] [-k KEY_VAULT] <hostname> [hostname...]
#
#   <hostname>    A name clients connect to. Give every one of them: the
#                 certificate is only valid for the names listed here. In the
#                 clusters that is `broker` plus the namespace FQDN; locally it
#                 is `broker`, `localhost` and `host.docker.internal`.
#                 Prefix with `IP:` for an address, e.g. `IP:127.0.0.1`.
#   -o            Where to write the credentials. Default ./mqtt-credentials.
#   -k            Key vault name, used only to print the `az keyvault secret set`
#                 commands for the generated secrets.
#
# The certificate lifetime is VALIDITY_DAYS, 825 by default — the longest a
# publicly trusted certificate may live, and short enough that rotation stays a
# routine rather than an emergency.

set -euo pipefail

OUTPUT_DIR=./mqtt-credentials
KEY_VAULT=""
VALIDITY_DAYS=${VALIDITY_DAYS:-825}

while getopts ":o:k:" option; do
    case "${option}" in
        o) OUTPUT_DIR=${OPTARG} ;;
        k) KEY_VAULT=${OPTARG} ;;
        *)
            echo "Unknown option -${OPTARG}" >&2
            exit 1
            ;;
    esac
done
shift $((OPTIND - 1))

if [ $# -eq 0 ]; then
    sed -n '2,20p' "$0" >&2
    exit 1
fi

BROKER_HOSTNAMES=("$@")
PRIMARY_HOSTNAME=${BROKER_HOSTNAMES[0]}

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
ACL_FILE="${SCRIPT_DIR}/../mosquitto/config/access_control"

# `IP:127.0.0.1` becomes `IP:127.0.0.1`, everything else becomes `DNS:<name>`.
subject_alternative_name() {
    local names=()
    local name
    for name in "${BROKER_HOSTNAMES[@]}"; do
        if [[ ${name} == IP:* ]]; then
            names+=("${name}")
        else
            names+=("DNS:${name}")
        fi
    done

    local joined
    joined=$(
        IFS=,
        echo "${names[*]}"
    )
    echo "subjectAltName=${joined}"
}

mkdir -p "${OUTPUT_DIR}"
chmod 0700 "${OUTPUT_DIR}"

echo "Generating CA..."
openssl req -x509 -newkey rsa:4096 -sha256 -days "${VALIDITY_DAYS}" -nodes \
    -keyout "${OUTPUT_DIR}/ca-key.pem" -out "${OUTPUT_DIR}/ca-cert.pem" \
    -subj "/CN=Flotilla MQTT CA ${PRIMARY_HOSTNAME}" 2>/dev/null
chmod 0600 "${OUTPUT_DIR}/ca-key.pem"

echo "Generating server certificate for ${BROKER_HOSTNAMES[*]}..."
openssl req -newkey rsa:4096 -sha256 -nodes \
    -keyout "${OUTPUT_DIR}/server-key.pem" -out "${OUTPUT_DIR}/server.csr" \
    -subj "/CN=${PRIMARY_HOSTNAME}" 2>/dev/null
chmod 0600 "${OUTPUT_DIR}/server-key.pem"

openssl x509 -req -in "${OUTPUT_DIR}/server.csr" -sha256 -days "${VALIDITY_DAYS}" \
    -CA "${OUTPUT_DIR}/ca-cert.pem" -CAkey "${OUTPUT_DIR}/ca-key.pem" -CAcreateserial \
    -out "${OUTPUT_DIR}/server-cert.pem" \
    -extfile <(
        subject_alternative_name
        printf "basicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n"
    ) 2>/dev/null

rm -f "${OUTPUT_DIR}/server.csr" "${OUTPUT_DIR}/ca-cert.srl"

echo "Generating passwords for the users in access_control..."
: > "${OUTPUT_DIR}/passwords"
chmod 0600 "${OUTPUT_DIR}/passwords"

MQTT_PASSWORDS=""
while read -r _ user; do
    # Hex, so alphanumeric: a comma would break MQTT_PASSWORDS apart, and the
    # passwords travel through env vars, .env files and az CLI arguments.
    password=$(openssl rand -hex 16)
    echo "${user}=${password}" >> "${OUTPUT_DIR}/passwords"
    MQTT_PASSWORDS="${MQTT_PASSWORDS}${MQTT_PASSWORDS:+,}${user}:${password}"
done < <(grep '^user ' "${ACL_FILE}")

echo "MQTT_PASSWORDS=${MQTT_PASSWORDS}" > "${OUTPUT_DIR}/mqtt-passwords.env"
chmod 0600 "${OUTPUT_DIR}/mqtt-passwords.env"

cat <<EOF

Written to ${OUTPUT_DIR}, valid until $(openssl x509 -in "${OUTPUT_DIR}/server-cert.pem" -noout -enddate | cut -d= -f2):

  ca-cert.pem          Not secret. Clients verify the broker against this.
                       ISAR reads it as ISAR_MQTT_CA_CERT.
  ca-key.pem           SECRET. Only needed to reissue server-cert.pem.
  server-cert.pem      Not secret. Broker reads it as TLS_SERVER_CERT.
  server-key.pem       SECRET. Broker reads it as TLS_SERVER_KEY.
  passwords            SECRET. One user=password line per ACL user.
  mqtt-passwords.env   SECRET. Broker reads it as MQTT_PASSWORDS.

Store the secrets in this environment's key vault. Do not commit them.
EOF

if [ -n "${KEY_VAULT}" ]; then
    isar_password=$(sed -n 's/^isar=//p' "${OUTPUT_DIR}/passwords")
    flotilla_password=$(sed -n 's/^flotilla=//p' "${OUTPUT_DIR}/passwords")

    cat <<EOF

Load them into ${KEY_VAULT} with:

  az keyvault secret set --vault-name ${KEY_VAULT} --name mqtt-broker-server-key --file ${OUTPUT_DIR}/server-key.pem
  az keyvault secret set --vault-name ${KEY_VAULT} --name mqtt-broker-server-cert --file ${OUTPUT_DIR}/server-cert.pem
  az keyvault secret set --vault-name ${KEY_VAULT} --name mqtt-broker-ca-cert --file ${OUTPUT_DIR}/ca-cert.pem
  az keyvault secret set --vault-name ${KEY_VAULT} --name mqtt-broker-passwords --value '${MQTT_PASSWORDS}'
  az keyvault secret set --vault-name ${KEY_VAULT} --name ISAR-MQTT-PASSWORD --value '${isar_password}'
  az keyvault secret set --vault-name ${KEY_VAULT} --name Mqtt--Password --value '${flotilla_password}'

SARA reads its own password from its own key vault (sara<env>-kv):

  az keyvault secret set --vault-name <sara-key-vault> --name Mqtt--Password --value '<the sara password above>'

The commands above put secrets in your shell history. Clear it afterwards.
EOF
fi
