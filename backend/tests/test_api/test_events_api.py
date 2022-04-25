import json
from datetime import datetime, timedelta
from http import HTTPStatus

import pytest
from fastapi import FastAPI, HTTPException
from fastapi.testclient import TestClient
from flotilla_openapi.models.event_request import EventRequest

from flotilla.database.crud import read_by_id, read_event_by_id
from flotilla.database.models import EventDBModel


@pytest.mark.parametrize(
    "query_params, expected_len",
    [
        (f"?robot_id=1&min_start_time={datetime.utcnow()-timedelta(hours=0.5)}", 1),
        (f"?min_start_time={datetime.min}", 0),
        (f"?min_start_time={datetime.utcnow()-timedelta(hours=0.5)}", 2),
        (f"?min_start_time={datetime.utcnow()-timedelta(days=6.9)}", 2),
    ],
)
def test_get_events(test_app: FastAPI, query_params: str, expected_len: int):

    with TestClient(test_app) as client:
        response = client.get("/events" + query_params)
        assert response.status_code == HTTPStatus.OK.value
        assert len(response.json()) == expected_len


@pytest.mark.parametrize(
    "event_id, expected_status_code",
    [
        (
            2,
            HTTPStatus.OK.value,
        ),
        (
            4,
            HTTPStatus.NOT_FOUND.value,
        ),
    ],
)
def test_get_event(
    test_app: FastAPI,
    event_id: int,
    expected_status_code: int,
):

    with TestClient(test_app) as client:
        response = client.get(f"/events/{event_id}")
        assert response.status_code == expected_status_code


@pytest.mark.parametrize(
    "event_request, expected_status_code",
    [
        (
            EventRequest(
                robot_id=1,
                mission_id=234,
                start_time=datetime.utcnow() + timedelta(days=10),
            ),
            HTTPStatus.CREATED.value,
        ),
        (
            EventRequest(
                robot_id=1,
                mission_id=234,
                start_time=datetime.utcnow() + timedelta(hours=0.5),
            ),
            HTTPStatus.CONFLICT.value,
        ),
    ],
)
def test_post_event(
    test_app: FastAPI, event_request: EventRequest, expected_status_code: int, session
):

    with TestClient(test_app) as client:
        response = client.post(
            f"/events",
            data=json.dumps(event_request.dict(), default=str),
        )
        assert response.status_code == expected_status_code
        if expected_status_code == HTTPStatus.CREATED.value:
            event = response.json()
            assert read_event_by_id(db=session, id=event.get("id"))


@pytest.mark.parametrize(
    "event_id, expected_status_code",
    [
        (
            1,
            HTTPStatus.NO_CONTENT.value,
        ),
        (
            5,
            HTTPStatus.NOT_FOUND.value,
        ),
    ],
)
def test_delete_event(
    test_app: FastAPI, event_id: int, expected_status_code: int, session
):

    with TestClient(test_app) as client:
        if expected_status_code == HTTPStatus.NO_CONTENT.value:
            assert read_by_id(EventDBModel, db=session, item_id=event_id)
        response = client.delete(f"/events/{event_id}")
        assert response.status_code == expected_status_code
        if expected_status_code == HTTPStatus.NO_CONTENT.value:
            with pytest.raises(HTTPException) as e:
                read_event_by_id(db=session, id=event_id)
                assert e.value.status_code == HTTPStatus.NOT_FOUND.value
