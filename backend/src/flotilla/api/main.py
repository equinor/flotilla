import time
from logging import getLogger
from logging.config import dictConfig
from threading import Thread

import uvicorn
from fastapi import FastAPI, Request, Response
from fastapi.middleware.cors import CORSMiddleware

from flotilla.api.authentication import authenticator
from flotilla.api.events_api import router as events_router
from flotilla.api.missions_api import router as missions_router
from flotilla.api.reports_api import router as reports_router
from flotilla.api.robots_api import router as robots_router
from flotilla.config.log_config import LogConfig
from flotilla.database.db import Base, SessionLocal, connection
from flotilla.database.mock_database.mock_database import populate_mock_db
from flotilla.services.events.event_handler import start_event_handler
from flotilla.settings import settings

app = FastAPI(
    swagger_ui_oauth2_redirect_url="/oauth2-redirect",
    swagger_ui_init_oauth={
        "usePkceWithAuthorizationCodeGrant": True,
        "clientId": settings.OPENAPI_CLIENT_ID,
    },
)

dictConfig(LogConfig().dict())

if settings.BACKEND_CORS_ORIGINS:
    app.add_middleware(
        CORSMiddleware,
        allow_origins=[str(origin) for origin in settings.BACKEND_CORS_ORIGINS],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )


@app.middleware("http")
async def log_api_endpoints(request: Request, call_next):
    logger = getLogger("api")
    start_time = time.time()
    request_string: str = f'"{request.method} {request.url}"'
    logger.info(f"Processing request: {request_string}")
    response: Response = await call_next(request)
    process_time = "{:.2f}".format((time.time() - start_time) * 1000)
    logger.info(
        f"Processed request: {request_string} - Result: {response.status_code} ({process_time} ms)"
    )
    return response


@app.on_event("startup")
async def load_config() -> None:
    await authenticator.load_config()


@app.on_event("startup")
def mock_db():
    populate_mock_db(session=SessionLocal(), connection=connection, base=Base)


@app.on_event("startup")
async def event_handler():
    thread = Thread(target = start_event_handler)
    thread.start()


app.include_router(robots_router)
app.include_router(missions_router)
app.include_router(reports_router)
app.include_router(events_router)


def run_app() -> None:
    uvicorn.run(app, host="localhost", reload=False)


if __name__ == "__main__":
    uvicorn.run(app, host="localhost", reload=False)
