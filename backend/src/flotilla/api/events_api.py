from datetime import timedelta
from http import HTTPStatus
from logging import getLogger
from typing import List

from fastapi import APIRouter, Body, Depends, HTTPException, Path, Response, Security
from flotilla_openapi.models.event import Event
from flotilla_openapi.models.event_request import EventRequest
from pytest import Session

from flotilla.api.authentication import authentication_scheme
from flotilla.api.pagination import PaginationParams
from flotilla.database.crud import (
    create_event,
    read_by_id,
    read_events_by_robot_id_and_time_span,
    read_list_paginated,
    remove_event,
)
from flotilla.database.db import get_db
from flotilla.database.models import EventDBModel

logger = getLogger("api")

router = APIRouter()

DEFAULT_EVENT_DURATION = timedelta(hours=1)


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
    description="""### Overview 
    Lookup events. The `start_time` defaults to the current time, `end_time` defaults to 
    `start_time` + 7 days. If no `robot_id` is provided, events for all enabled robots on the asset is included.""",
    dependencies=[Security(authentication_scheme)],
)
async def get_events(
    db: Session = Depends(get_db),
    params: PaginationParams = Depends(),
) -> List[Event]:
    """Lookup events."""
    db_events: List[EventDBModel] = read_list_paginated(
        db=db, params=params, modelType=EventDBModel
    )
    events: List[Event] = [event.get_api_event() for event in db_events]
    return events


@router.post(
    "/events",
    responses={
        HTTPStatus.CREATED.value: {"model": Event, "description": "Request successful"},
    },
    tags=["Events"],
    summary="Create new event",
    description="""### Overview 
    Adds a new event to the robot schedule. New entries to the schedule can be added as 
    long as they do not conflict with already scheduled events.""",
    dependencies=[Security(authentication_scheme)],
)
async def post_event(
    response: Response,
    db: Session = Depends(get_db),
    event_request: EventRequest = Body(None, description="Time entry update"),
) -> Event:
    """Add a new event to the robot schedule"""
    try:
        end_time = event_request.start_time + DEFAULT_EVENT_DURATION
        overlapping_events: List[Event] = read_events_by_robot_id_and_time_span(
            db=db,
            robot_id=event_request.robot_id,
            start_time=event_request.start_time,
            end_time=end_time,
        )
        if overlapping_events:
            raise HTTPException(
                status_code=HTTPStatus.CONFLICT.value,
                detail=f"Conflict with already existing event in the same time period. Events with id: {','.join(str(event.id) for event in overlapping_events)}",
            )
        event_id: int = create_event(
            db,
            event_request.robot_id,
            event_request.mission_id,
            event_request.start_time,
            DEFAULT_EVENT_DURATION,
        )
        db_event: EventDBModel = read_by_id(EventDBModel, db, event_id)
        event: Event = db_event.get_api_event()
    except HTTPException as e:
        logger.error(f"An error occured while creating an event: {e.detail}")
        raise
    response.status_code = HTTPStatus.CREATED.value
    return event


@router.delete(
    "/events/{event_id}",
    responses={
        HTTPStatus.NO_CONTENT.value: {"description": "Event successfully deleted"},
    },
    tags=["Events"],
    summary="Delete event with specified id",
    description="""### Overview
    Deletes an event from the robot schedule. Can only be used for events that have not started yet.""",
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
    except HTTPException as e:
        logger.error(f"Failed to delete event with id {event_id}: {e.detail}")
        raise
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
    description="""### Overview
    Lookup event with specified id. Can only be used for events that have not started yet.""",
    dependencies=[Security(authentication_scheme)],
)
async def get_event(
    response: Response,
    db: Session = Depends(get_db),
    event_id: int = Path(None, description=""),
) -> Event:
    """Lookup event with specified id. Can only be used for events that have not started yet."""
    try:
        db_event: EventDBModel = read_by_id(EventDBModel, db, event_id)
        event: Event = db_event.get_api_event()
    except HTTPException as e:
        logger.error(f"Failed to get event with id {event_id}: {e.detail}")
        raise
    return event
