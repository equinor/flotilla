# Mosquitto MQTT Broker

## Running the broker

Start by building the docker image

```
docker build -t flotilla-broker .
```

The broker may be started as

```
docker run -it -p 1883:1883 -p 9001:9001 flotilla-broker:latest
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

## Using broker locally

### Installation

To install the Mosquitto broker run the following commands

```
sudo apt-add-repository ppa:mosquitto-dev/mosquitto-ppa
sudo apt-get update
sudo apt-get install mosquitto
sudo apt-get install mosquitto-clients
```

The broker may be started with

```
mosquitto -p 1883 -c mosquitto/config/mosquitto.conf
```

### Testing the broker

To test that the broker functions as expected the `mosquitto_sub` and `mosquitto_pub` tools that are wrapped in the installation may be used.
First, subscribe to a topic

```
mosquitto_sub -t test -u mosquitto -P default
```

Then attempt to publish something to the same topic in a different terminal

```
mosquitto_pub -t test -m hei -u mosquitto -P default
```
