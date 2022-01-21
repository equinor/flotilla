import pytest
from sqlalchemy.orm import Session

from flotilla.database.db import Base, SessionLocal, engine
from flotilla.database.mock_database.mock_database import populate_mock_db

mock_session: Session = SessionLocal()
populate_mock_db(session=mock_session, engine=engine, base=Base)


@pytest.fixture()
def session() -> Session:
    return SessionLocal()
