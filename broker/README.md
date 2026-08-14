# Mosquitto MQTT broker

The Flotilla MQTT broker is a [Mosquitto](https://mosquitto.org/) broker with TLS and role-based authentication.

For authentication details, testing, and certificate management, see [best_practices.md](./best_practices.md).

## Prerequisites

- [Docker](https://docs.docker.com/engine/install/) and [Docker Compose](https://docs.docker.com/compose/install/)

## Configuration

The broker expects the private key for its server x509 certificate, used for TLS, in the environment variable `TLS_SERVER_KEY`. This is a secret and should be treated as such — it is found in our key vault.

Create the configuration by running [`setup.sh`](../setup.sh) from the repository root, which prompts you for the key. Alternatively, copy the template and fill it in manually:

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
