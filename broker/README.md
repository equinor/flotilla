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

The broker has been setup with a default configuration where the username and password is

```
username = mosquitto
password = default
```

To change the password replace the [password file](./passwd_file) with a new file containing a username and password.
See [this guide](https://mosquitto.org/documentation/authentication-methods/) on how to manage the password file.

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
