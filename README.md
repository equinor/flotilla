# Flotilla

Flotilla is the main point of access for operators to interact with multiple robots in a facility. The application consists of a [frontend](frontend/README.md) in React and a [backend](backend/README.md) in Python using the FastAPI framework.

## Setup

For development, please fork the repository. Then, clone the repository:

```
git clone https://github.com/equinor/flotilla
```

## Run with docker

Install docker using the [official documentation](https://docs.docker.com/engine/install/ubuntu/).

Install docker compose:
```
sudo apt update
sudo apt install docker-compose
```
Build the docker container:
```
sudo docker-compose build
```

Start Flotilla by running:
```
sudo docker-compose up
```
or
```
sudo docker-compose up --build
```
