import pytest
from fastapi import FastAPI
from sqlalchemy.orm import Session

from flotilla.api.authentication import Authenticator, authentication_scheme
from flotilla.database.db import Base, SessionLocal, engine
from flotilla.database.mock_database.mock_database import populate_mock_db
from flotilla.main import app
from flotilla.services.echo import get_echo_service
from flotilla.services.isar import get_isar_service
from tests.mocks.echo_service_mock import get_echo_service_mock
from tests.mocks.isar_service_mock import get_isar_service_mock

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
    app.dependency_overrides[get_echo_service] = get_echo_service_mock
    app.dependency_overrides[get_isar_service] = get_isar_service_mock
    return app
