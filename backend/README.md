# Flotilla backend

## Installation

```bash
pip install -e .[dev]
pip install -i https://test.pypi.org/simple/ flotilla-openapi
```

## Running the API

Start the API by running

```bash
uvicorn flotilla.main:app --reload
```

The API is then available on
http://127.0.0.1:8000/docs

## Running the tests

The tests can be run with

```bash
pytest
```
