import pytest
from azure.core.credentials import AccessToken
from fastapi import FastAPI
from pytest_mock import MockerFixture
from sqlalchemy.orm import Session, scoped_session, sessionmaker

from flotilla.api.authentication import Authenticator, authentication_scheme
from flotilla.database.db import Base, SessionLocal, connection
from flotilla.database.mock_database.mock_database import populate_mock_db
from flotilla.main import app


@pytest.fixture(scope="session")
def setup_database():
    populate_mock_db(session=SessionLocal(), connection=connection, base=Base)
    yield
    Base.metadata.drop_all()


@pytest.fixture
def session(setup_database):
    transaction = connection.begin()
    yield scoped_session(
        sessionmaker(autocommit=False, autoflush=False, bind=connection)
    )
    transaction.rollback()


@pytest.fixture()
def test_app(mocker: MockerFixture) -> FastAPI:
    app.dependency_overrides[authentication_scheme] = Authenticator(
        authentication_enabled=False
    ).get_scheme()
    mocker.patch(
        "azure.identity.DefaultAzureCredential.get_token"
    ).return_value = AccessToken(token="mock_token", expires_on=0)
    return app
