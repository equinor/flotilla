# Mosquitto MQTT Broker

## Set up the broker

The broker expects a private key for its server x509 certificate used for TLS.
This must be provided through an environment variable called `FLOTILLA_BROKER_SERVER_KEY`.
This is a secret, and should be treated as such. It can be found in our keyvault.

### Automatic environment setup

See [Flotilla readme](../README.md#automatic-environment-setup)

### Manual environment setup

The best way to pass this is to store it in a `.env` file in the root of flotilla, and docker compose loads this by default on startup.
See [Using the “--env-file” option](https://docs.docker.com/compose/environment-variables/#using-the---env-file--option) for more information.

## Running the broker

From the flotilla root directory, run the following command:

```
docker compose up --build broker
```

## Authentication

The broker has been setup with role based authentication.
The current roles with their permissions per topic are described in the table below.

|          | read   | write  |
| -------- | ------ | ------ |
| admin    | #      | #      |
| flotilla | isar/# | -      |
| isar     | isar/# | isar/# |

To change the password see [this guide](https://mosquitto.org/documentation/authentication-methods/)
on how to manage the [password file](./mosquitto/config/passwd_file).

To change the roles see [this guide]()
on how to manage the role based access control.
They are defined in the [access_control file](./mosquitto/config/access_control).

## Installing broker outside of docker

To test the dockerized broker, the functions `mosquitto_sub` and `mosquitto_pub` are useful.
To gain access to them on your machine you will need to install the mosquitto broker.

### Linux installation

To install the Mosquitto broker run the following commands

```
sudo apt-add-repository ppa:mosquitto-dev/mosquitto-ppa
sudo apt-get update
sudo apt-get install mosquitto
sudo apt-get install mosquitto-clients
```

### Windows installation

Go to the [official Mosquitto download page](https://mosquitto.org/download/) and download and install the
binaries for windows.

Then add the installation folder to your PATH variable for the commands to be available from your terminal.

### Start non-docker broker

If running the broker outside docker, you will need to manually create the `server-key.pem` file, containing the secret server key
mentioned in the [setup section](#set-up-the-broker).
The mosquitto config file expects this file to be stored in the [mosquitto/config/certs](./mosquitto/config/certs) folder.

The broker may then be started with

```
mosquitto -p 1883 -c mosquitto/config/mosquitto.conf
```

## Testing the broker

To test that the broker functions as expected the `mosquitto_sub` and `mosquitto_pub` tools that are wrapped in the installation may be used.
For access to all topics, you need to use the admin user.
The password for the admin can be found in our keyvault.

For the TLS encryption, you will need to reference the CA certificate. This is not a secret and can be found in [mosquitto/config/certs](./mosquitto/config/certs).

First, subscribe to a topic

```
mosquitto_sub -t topic_name -u admin -P secret_password --cafile ca-cert.pem
```

Then attempt to publish something to the same topic in a different terminal

```
mosquitto_pub -t topic_name -m hei -u admin -P secret_password --cafile ca-cert.pem
```
