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

## Overwriting settings with environment variables

The default settings [settings](src/flotilla/settings/settings.py) can be overwritten by environment variables using the prefix `FLOTILLA_`

```
export FLOTILLA_<VAR_NAME>=some_value
```

## API authentication

To run the app locally and connect to upstream services, the following environment variables need to be set for authentication.

```
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_CLIENT_SECRET
```
