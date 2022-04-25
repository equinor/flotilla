from threading import Thread
from typing import List

from flotilla.api.main import run_app
from flotilla.database.db import Base, SessionLocal, connection
from flotilla.database.mock_database.mock_database import populate_mock_db
from flotilla.services.events.event_handler import run_event_handler

if __name__ == "__main__":
    threads: List[Thread] = []

    populate_mock_db(session=SessionLocal(), connection=connection, base=Base)

    api_thread: Thread = Thread(
        target=run_app,
        name="Flotilla Backend API",
        daemon=True,
    )
    threads.append(api_thread)

    event_handler_thread: Thread = Thread(
        target=run_event_handler, name="Event handler", daemon=True
    )
    threads.append(event_handler_thread)

    for thread in threads:
        thread.start()

    for thread in threads:
        thread.join()
