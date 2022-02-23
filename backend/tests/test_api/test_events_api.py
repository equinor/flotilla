import json
from datetime import datetime
from http import HTTPStatus

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from flotilla_openapi.models.event_request import EventRequest


def test_get_events(test_app: FastAPI):

    with TestClient(test_app) as client:
        response = client.get("/events")
        assert response.status_code == HTTPStatus.OK.value


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
            EventRequest(robot_id=1, mission_id=234, start_time=datetime.now()),
            HTTPStatus.CREATED.value,
        ),
    ],
)
def test_post_event(
    test_app: FastAPI,
    event_request: EventRequest,
    expected_status_code: int,
):

    with TestClient(test_app) as client:
        response = client.post(
            f"/events",
            data=json.dumps(event_request.dict(), default=str),
        )
        assert response.status_code == expected_status_code


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
    test_app: FastAPI,
    event_id: int,
    expected_status_code: int,
):

    with TestClient(test_app) as client:
        response = client.delete(f"/events/{event_id}")
        assert response.status_code == expected_status_code
