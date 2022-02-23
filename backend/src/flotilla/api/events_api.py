from http import HTTPStatus
from logging import getLogger
from typing import List

from fastapi import APIRouter, Body, Depends, Path, Response, Security
from flotilla_openapi.models.event import Event
from flotilla_openapi.models.event_request import EventRequest
from flotilla_openapi.models.problem_details import ProblemDetails
from pytest import Session

from flotilla.api.authentication import authentication_scheme
from flotilla.database.crud import (
    DBException,
    create_event,
    read_event_by_id,
    read_events,
    remove_event,
)
from flotilla.database.db import get_db
from flotilla.database.models import EventDBModel

logger = getLogger("api")

router = APIRouter()

NOT_FOUND_DESCRIPTION = "Not Found - No event with given id"
INTERNAL_SERVER_ERROR_DESCRIPTION = "Internal Server Error"


@router.get(
    "/events",
    responses={
        HTTPStatus.OK.value: {
            "model": List[Event],
            "description": "Request successful.",
        },
    },
    tags=["Events"],
    summary="Lookup events",
    dependencies=[Security(authentication_scheme)],
)
async def get_events(
    db: Session = Depends(get_db),
) -> List[Event]:
    """Lookup events."""
    db_events: List[EventDBModel] = read_events(db)
    events: List[Event] = [event.get_api_event() for event in db_events]
    return events


@router.post(
    "/events",
    responses={
        HTTPStatus.CREATED.value: {"model": Event, "description": "Request successful"},
    },
    tags=["Events"],
    summary="Create new event",
    dependencies=[Security(authentication_scheme)],
)
async def post_event(
    response: Response,
    db: Session = Depends(get_db),
    event_request: EventRequest = Body(None, description="Time entry update"),
) -> Event:
    """Add a new event to the robot schedule"""
    try:
        event_id: int = create_event(
            db,
            event_request.robot_id,
            event_request.mission_id,
            event_request.start_time,
        )
        db_event: EventDBModel = read_event_by_id(db, event_id)
        event: EventDBModel = db_event.get_api_event()
    except DBException:
        logger.exception(f"An error occured while creating event.")
        response.status_code = HTTPStatus.INTERNAL_SERVER_ERROR.value
        return ProblemDetails(title=INTERNAL_SERVER_ERROR_DESCRIPTION)
    response.status_code = HTTPStatus.CREATED.value
    return event


@router.delete(
    "/events/{event_id}",
    responses={
        HTTPStatus.NO_CONTENT.value: {"description": "Event successfully deleted"},
    },
    tags=["Events"],
    summary="Delete event with specified id",
    dependencies=[Security(authentication_scheme)],
)
async def delete_event(
    response: Response,
    db: Session = Depends(get_db),
    event_id: int = Path(None, description=""),
) -> None:
    """Deletes an event from the robot schedule. Can only be used for events that have not started yet."""
    try:
        remove_event(db, event_id)
    except DBException:
        logger.exception(f"Could not delete event with id {event_id}.")
        response.status_code = HTTPStatus.NOT_FOUND.value
        return ProblemDetails(title=NOT_FOUND_DESCRIPTION)
    response.status_code = HTTPStatus.NO_CONTENT.value
    return None


@router.get(
    "/events/{event_id}",
    responses={
        HTTPStatus.OK.value: {
            "model": Event,
            "description": "Request successful.",
        },
    },
    tags=["Events"],
    summary="Lookup event with specified id",
    dependencies=[Security(authentication_scheme)],
)
async def get_event(
    response: Response,
    db: Session = Depends(get_db),
    event_id: int = Path(None, description=""),
) -> Event:
    """Lookup event with specified id. Can only be used for events that have not started yet."""
    try:
        db_event: EventDBModel = read_event_by_id(db, event_id)
        event: EventDBModel = db_event.get_api_event()
    except DBException:
        logger.exception(f"Could not get event with id {event_id}.")
        response.status_code = HTTPStatus.NOT_FOUND.value
        return ProblemDetails(title=NOT_FOUND_DESCRIPTION)
    return event
