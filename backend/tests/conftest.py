import pytest
from azure.core.credentials import AccessToken
from fastapi import FastAPI
from pytest_mock import MockerFixture
from sqlalchemy.orm import scoped_session, sessionmaker

from flotilla.api.authentication import Authenticator, authentication_scheme
from flotilla.api.main import app
from flotilla.database.db import Base, SessionLocal, connection, get_db
from flotilla.database.mock_database.mock_database import populate_mock_db


# Database setup is run once per test session
@pytest.fixture(scope="session")
def setup_database():
    populate_mock_db(session=SessionLocal(), connection=connection, base=Base)
    yield
    Base.metadata.drop_all()


# A scoped session is setup once per test and the changes made in a test are deleted after the test
@pytest.fixture
def session(setup_database):
    transaction = connection.begin()
    yield scoped_session(
        sessionmaker(autocommit=False, autoflush=False, bind=connection)
    )
    transaction.rollback()


@pytest.fixture()
def test_app(session, mocker: MockerFixture) -> FastAPI:
    app.dependency_overrides[authentication_scheme] = Authenticator(
        authentication_enabled=False
    ).get_scheme()
    mocker.patch(
        "azure.identity.DefaultAzureCredential.get_token"
    ).return_value = AccessToken(token="mock_token", expires_on=0)
    # This causes the endpoints called by TestClient to use the same transactional database as the other tests
    app.dependency_overrides[get_db] = lambda: session()
    return app
