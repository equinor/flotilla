from logging.config import dictConfig

import uvicorn
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from flotilla.api.authentication import authenticator
from flotilla.api.events_api import router as events_router
from flotilla.api.missions_api import router as missions_router
from flotilla.api.reports_api import router as reports_router
from flotilla.api.robots_api import router as robots_router
from flotilla.config.log_config import LogConfig
from flotilla.database.db import Base, SessionLocal, engine
from flotilla.database.mock_database.mock_database import populate_mock_db
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


@app.on_event("startup")
async def load_config() -> None:
    await authenticator.load_config()


@app.on_event("startup")
def startup_event():
    populate_mock_db(session=SessionLocal(), engine=engine, base=Base)


app.include_router(robots_router)
app.include_router(missions_router)
app.include_router(reports_router)
app.include_router(events_router)


if __name__ == "__main__":
    uvicorn.run(app, host="localhost", reload=False)
