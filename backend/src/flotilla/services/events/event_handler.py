from datetime import datetime
from logging import getLogger

logger = getLogger("event handler")


def start_event_handler():
    t = datetime.now()
    logger.info(f"Event handler started {t}")
