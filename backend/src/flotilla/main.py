from http.client import HTTPException

from fastapi import FastAPI
from flotilla_openapi.models.problem_details import ProblemDetails
from sqlalchemy.orm import Session

from flotilla.api.robots_api import router as robots_router
from flotilla.database.db import Base, SessionLocal, engine
from flotilla.database.mock_database.mock_database import populate_mock_db

app = FastAPI()


@app.on_event("startup")
def startup_event():
    populate_mock_db(session=SessionLocal(), engine=engine, base=Base)


app.include_router(robots_router)
