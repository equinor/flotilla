from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import scoped_session, sessionmaker

from flotilla.settings import settings

connection = create_engine(
    settings.SQLALCHEMY_DATABASE_URL, connect_args={"check_same_thread": False}
).connect()

session_factory = sessionmaker(autocommit=False, autoflush=False, bind=connection)

SessionLocal = scoped_session(session_factory=session_factory)

Base = declarative_base()


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
