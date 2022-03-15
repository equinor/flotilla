import pytest
from azure.core.credentials import AccessToken
from fastapi import FastAPI
from pytest_mock import MockerFixture
from sqlalchemy.orm import Session

from flotilla.api.authentication import Authenticator, authentication_scheme
from flotilla.database.db import Base, SessionLocal, engine
from flotilla.database.mock_database.mock_database import populate_mock_db
from flotilla.main import app

mock_session: Session = SessionLocal()
populate_mock_db(session=mock_session, engine=engine, base=Base)


@pytest.fixture()
def session() -> Session:
    return SessionLocal()


@pytest.fixture()
def test_app(mocker: MockerFixture) -> FastAPI:
    app.dependency_overrides[authentication_scheme] = Authenticator(
        authentication_enabled=False
    ).get_scheme()
    mocker.patch(
        "azure.identity.DefaultAzureCredential.get_token"
    ).return_value = AccessToken(token="mock_token", expires_on=0)
    return app
