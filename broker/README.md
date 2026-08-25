# Mosquitto MQTT broker

The Flotilla MQTT broker is a [Mosquitto](https://mosquitto.org/) broker with TLS and role-based authentication.

For authentication details, testing, and certificate management, see [best_practices.md](./best_practices.md).

## Prerequisites

- [Docker](https://docs.docker.com/engine/install/) and [Docker Compose](https://docs.docker.com/compose/install/)

## Configuration

The broker assembles its identity material — TLS certificates and the password file — on
startup, from the environment. Anything not supplied falls back to the defaults committed to
this repository, so existing deployments keep working unchanged.

| Variable | Secret | Meaning |
| --- | --- | --- |
| `MQTT_PASSWORDS` | yes | Comma separated `user:password` pairs. Must cover every user in [`access_control`](./mosquitto/config/access_control). Falls back to the committed `passwd_file`. |
| `TLS_SERVER_KEY` | yes | Private key for the server certificate, as PEM or as the bare base64 body. Required unless TLS is disabled. |
| `TLS_SERVER_CERT` | no | Server certificate as PEM. Falls back to the committed one. |
| `TLS_CA_CERT` | no | CA certificate as PEM. Falls back to the committed one. |
| `MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS` | no | `true` disables TLS. Authentication and ACLs are still enforced. Local development only. |

Secrets belong in the key vault of the environment the broker runs in, never in this
repository.

### Minting credentials for an environment

Each environment should have its own credentials, so that a compromise in one does not reach
another:

```bash
broker/scripts/generate-mqtt-credentials.sh <broker-hostname> <output-directory>
```

`<broker-hostname>` must be the host clients connect to — the certificate is issued for it.
The script writes a CA, a server certificate and a random password per ACL user, and prints
which of them are secret. Store those in that environment's key vault.

Clients need the matching CA certificate to verify the broker: ISAR reads it from
`ISAR_MQTT_CA_CERT`. Flotilla and SARA do not verify the certificate chain.

### Local development

Create the configuration by running [`setup.sh`](../setup.sh) from the repository root, which
prompts you for the key. Alternatively, copy the template and fill it in manually:

```bash
cp .env.example .env
```

Docker Compose loads `broker/.env` by default on startup. See [Using the "--env-file" option](https://docs.docker.com/compose/environment-variables/#using-the---env-file--option) for more information.

## Run

From the repository root:

```bash
make broker      # docker compose up broker --build
```

If the address is already in use, you may have to kill an existing Mosquitto process:

```bash
sudo pkill mosquitto
```
