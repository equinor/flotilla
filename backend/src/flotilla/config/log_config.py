from pydantic import BaseModel


class LogConfig(BaseModel):
    """Configuration for the backend api logger"""

    LOGGER_NAME: str = "api"
    LOG_FORMAT: str = "%(levelprefix)s %(asctime)s | %(message)s"
    LOG_FORMAT_FILE: str = "%(asctime)s | %(levelname)s: %(message)s"
    LOG_LEVEL: str = "DEBUG"

    version = 1
    disable_existing_loggers = False
    formatters = {
        "default": {
            "()": "uvicorn.logging.DefaultFormatter",
            "fmt": LOG_FORMAT,
            "datefmt": "%Y-%m-%d %H:%M:%S",
        },
        "file_friendly": {
            "()": "uvicorn.logging.DefaultFormatter",
            "fmt": LOG_FORMAT_FILE,
            "datefmt": "%Y-%m-%d %H:%M:%S",
        },
    }
    handlers = {
        "api file": {
            "formatter": "file_friendly",
            "class": "logging.FileHandler",
            "filename": "api.log",
        },
        "api stream": {
            "formatter": "default",
            "class": "logging.StreamHandler",
            "stream": "ext://sys.stderr",
        },
    }
    loggers = {
        "api": {"handlers": ["api file", "api stream"], "level": LOG_LEVEL},
    }
