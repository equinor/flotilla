# Flotilla backend

## Installation

```bash
pip install -e .[dev]
pip install -U -i https://test.pypi.org/simple/ flotilla-openapi
```

## Running the API

Start the API by running

```bash
python main.py
```

The API is available at `localhost:8000/docs`.

## Running the tests

The tests can be run with

```bash
pytest .
```

## API authentication

To run the app locally and connect to upstream services, the following environment variables need to be set for authentication.

```
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_CLIENT_SECRET
```
