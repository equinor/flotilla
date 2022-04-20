from logging import getLogger
from typing import List

from pytest import Session

from flotilla.database.crud import read_list_paginated
from flotilla.database.db import SessionLocal
from flotilla.database.models import EventDBModel

logger = getLogger("event handler")


def start_event_handler():
    logger.info(f"Event handler started")
    db_session: Session = SessionLocal()

    db_events: List[EventDBModel] = read_list_paginated(
        db=db_session, modelType=EventDBModel
    )

    SessionLocal.remove()
    logger.info(db_events)
