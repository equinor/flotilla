# Flotilla

Flotilla is the main point of access for operators to interact with multiple robots in a facility. The application
consists of a [frontend](frontend) in React, a [backend](backend) in ASP.NET and a Mosquitto MQTT [Broker](broker).

## Setup

For development, please fork the repository. Then, clone the repository:

```
git clone https://github.com/equinor/flotilla
```

Please see separate installation guides for [frontend](frontend), [backend](backend), and [Broker](broker).  
For the environment setup, either run the script as described below or do it manually as described in each component.

### Automatic environment setup

Run the [setup.sh](./setup.sh) to automatically set up your dev environment for the components.  
This script will ask you for the `Client Secret` for the backend and the `MQTT broker server key` for the MQTT broker.

## Run with docker

Install docker using the [official documentation](https://docs.docker.com/engine/install/ubuntu/).

Install docker compose:

```
sudo apt update
sudo apt install docker-compose
```

Build the docker container:

```
docker-compose build
```

Setup a .env file in the backend directory with the following environment variables:

```
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_CLIENT_SECRET
```

Start Flotilla by running:

```
docker-compose up
```

or

```
docker-compose up --build
```

## Contributing

We welcome all kinds of contributions, including code, bug reports, issues, feature requests, and documentation. The preferred way of submitting a contribution is to either make an [issue](https://github.com/equinor/isar/issues) on github or by forking the project on github and making a pull requests.

We write our commit messages according to [this guide](https://cbea.ms/git-commit/).
