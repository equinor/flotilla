# Broker best practices

Reference material for the Flotilla MQTT broker. For installation and how to run the broker, see the [README](./README.md).

## Authentication

The broker has been set up with role-based authentication. The current roles with their permissions per topic are described in the table below.

|          | read   | write  |
| -------- | ------ | ------ |
| admin    | #      | #      |
| flotilla | isar/# | -      |
| isar     | isar/# | isar/# |

To change the password see [this guide](https://mosquitto.org/documentation/authentication-methods/) on how to manage the [password file](./mosquitto/config/passwd_file). Note that when `MQTT_PASSWORDS` is set the broker generates its password file on startup instead, and the committed one is unused.

To change the roles see [this guide](https://mosquitto.org/documentation/authentication-methods/) on how to manage the role-based access control. They are defined in the [access_control file](./mosquitto/config/access_control).

## Installing the broker outside of Docker

To test the dockerized broker, the functions `mosquitto_sub` and `mosquitto_pub` are useful. To gain access to them on your machine you will need to install the Mosquitto broker.

### Linux

```bash
sudo apt-add-repository ppa:mosquitto-dev/mosquitto-ppa
sudo apt-get update
sudo apt-get install mosquitto
sudo apt-get install mosquitto-clients
```

### Windows

Go to the [official Mosquitto download page](https://mosquitto.org/download/) and download and install the binaries for Windows.

Then add the installation folder to your PATH variable for the commands to be available from your terminal.

### Starting a non-Docker broker

If running the broker outside Docker, you will need to manually create the `server-key.pem` file, containing the secret server key described in the [README](./README.md#configuration). The Mosquitto config file expects this file to be stored in the [mosquitto/config/certs](./mosquitto/config/certs) folder.

The broker may then be started with:

```bash
mosquitto -p 1883 -c mosquitto/config/mosquitto.conf
```

## Testing the broker

To test that the broker functions as expected, the `mosquitto_sub` and `mosquitto_pub` tools may be used. For access to all topics, you need to use the admin user. The password for the admin can be found in our key vault.

For the TLS encryption, you will need to reference the CA certificate. This is not a secret and can be found in [mosquitto/config/certs](./mosquitto/config/certs).

First, subscribe to a topic:

```bash
mosquitto_sub -t topic_name -u admin -P secret_password --cafile ca-cert.pem
```

Then attempt to publish something to the same topic in a different terminal:

```bash
mosquitto_pub -t topic_name -m hei -u admin -P secret_password --cafile ca-cert.pem
```

## Certificates

Each environment should have its own CA and server certificate, minted with
[`scripts/generate-mqtt-credentials.sh`](./scripts/generate-mqtt-credentials.sh) and stored in
that environment's key vault. See the [README](./README.md#minting-credentials-for-an-environment).

The certificates committed to [mosquitto/config/certs](./mosquitto/config/certs) are the
fallback used when `TLS_SERVER_CERT` and `TLS_CA_CERT` are not set. They are being phased out
in favour of per-environment credentials.
