# Mosquitto MQTT broker

The Flotilla MQTT broker is a [Mosquitto](https://mosquitto.org/) broker with TLS and role-based authentication.

For authentication details, testing, and certificate management, see [best_practices.md](./best_practices.md).

## Prerequisites

- [Docker](https://docs.docker.com/engine/install/) and [Docker Compose](https://docs.docker.com/compose/install/)

## Configuration

The broker assembles its identity material — TLS certificates and the password file — on
startup, from the environment. Anything not supplied falls back to the defaults committed to
this repository, so existing deployments keep working unchanged. Every fallback is logged, and
`MQTT_REQUIRE_ENV_CREDENTIALS=true` turns it into a startup failure instead.

| Variable | Secret | Meaning |
| --- | --- | --- |
| `MQTT_PASSWORDS` | yes | Comma separated `user:password` pairs. Must cover every user in [`access_control`](./mosquitto/config/access_control), and the broker refuses to start if it does not. A password may contain a colon but not a comma. Falls back to the committed `passwd_file`. |
| `TLS_SERVER_KEY` | yes | Private key for the server certificate, as PEM or as the bare base64 body. Required unless TLS is disabled. |
| `TLS_SERVER_CERT` | no | Server certificate, as PEM or as the bare base64 body. Falls back to the committed one. |
| `TLS_CA_CERT` | no | CA certificate, as PEM or as the bare base64 body. Falls back to the committed one. |
| `MQTT_REQUIRE_ENV_CREDENTIALS` | no | `true` refuses to start if anything fell back to a committed default. **Every deployed environment sets this.** |
| `MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS` | no | `true` disables TLS. Authentication and ACLs are still enforced. Local development only, and mutually exclusive with the above. |

Secrets belong in the key vault of the environment the broker runs in, never in this
repository.

### Minting credentials for an environment

Each environment should have its own credentials, so that a compromise in one does not reach
another:

```bash
broker/scripts/generate-mqtt-credentials.sh -o <output-directory> -k <key-vault> <hostname> [hostname...]
```

List **every** name clients connect to — the certificate is valid for those and no others. In
the clusters that is `broker` and the namespace FQDN; locally it is `broker`, `localhost` and
`host.docker.internal`. Prefix an address with `IP:`.

```bash
broker/scripts/generate-mqtt-credentials.sh -o ./dev -k robotics-dev-kv \
    broker broker.robotics.svc.cluster.local
```

The script writes a CA, a server certificate and a random password per ACL user, says which of
them are secret, and prints the `az keyvault secret set` commands to load them. Certificates
are valid for `VALIDITY_DAYS`, 825 by default.

Clients need the matching CA certificate to verify the broker: ISAR reads it from
`ISAR_MQTT_CA_CERT`. Flotilla and SARA set `IgnoreCertificateChainErrors`, so they do not
verify it — worth fixing, but it means they only need their password.

### Local development

Run [`setup.sh`](../setup.sh) from the repository root. It generates throwaway credentials into
`broker/.local-credentials` — nothing shared with a deployed environment ever reaches a laptop —
writes `broker/.env`, and prints the backend's MQTT password for the ASP.NET Secret Manager.
Alternatively, copy the template and fill it in manually:

```bash
cp .env.example .env
```

`broker/.env` is read through Compose's `env_file`, which cannot hold a multi-line value, so the
PEM values go in as the base64 body on a single line. The broker reassembles them.

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
