import pytest
from fastapi import FastAPI
from sqlalchemy.orm import Session

from flotilla.api.authentication import Authenticator, authentication_scheme
from flotilla.database.db import Base, SessionLocal, engine
from flotilla.database.mock_database.mock_database import populate_mock_db
from flotilla.echo.requests import get_echo_requests
from flotilla.main import app
from tests.mocks.echo_requests_mock import get_echo_requests_mock

mock_session: Session = SessionLocal()
populate_mock_db(session=mock_session, engine=engine, base=Base)


@pytest.fixture()
def session() -> Session:
    return SessionLocal()


@pytest.fixture()
def test_app() -> FastAPI:
    app.dependency_overrides[authentication_scheme] = Authenticator(
        authentication_enabled=False
    ).get_scheme()
    app.dependency_overrides[get_echo_requests] = get_echo_requests_mock
    return app
