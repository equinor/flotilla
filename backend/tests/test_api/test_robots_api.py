from http import HTTPStatus

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from requests import Response

from flotilla.services.isar import get_isar_service
from tests.mocks.isar_requests_mock import IsarServiceMock


def setup_isar_service_start(response: Response):
    isar_service_mock: IsarServiceMock = IsarServiceMock()
    isar_service_mock.add_start_mission_responses([response])

    def get_isar_requests_mock():
        yield isar_service_mock

    return get_isar_requests_mock


def setup_isar_service_stop(response: Response):
    isar_request_mock: IsarServiceMock = IsarServiceMock()
    isar_request_mock.add_stop_responses([response])

    def get_isar_requests_mock():
        yield isar_request_mock

    return get_isar_requests_mock


def test_get_robots(test_app: FastAPI):
    with TestClient(test_app) as client:
        response = client.get("/robots")
        assert response.status_code == HTTPStatus.OK.value
        assert len(response.json()) == 2


@pytest.mark.parametrize(
    "robot_id, expected_status_code",
    [
        (1, HTTPStatus.OK.value),
        (23, HTTPStatus.NOT_FOUND.value),
    ],
)
def test_get_robot(test_app: FastAPI, robot_id: int, expected_status_code: int):
    with TestClient(test_app) as client:
        response = client.get(f"/robots/{robot_id}")
        assert response.status_code == expected_status_code


json_successful_start_request = {
    "message": "Mission started",
    "started": True,
    "mission_id": "KAA129423",
}

json_unsuccessful_start_request = {
    "message": "Mission in progress",
    "started": False,
    "mission_id": None,
}

successful_start_response: Response = Response()
successful_start_response.status_code = HTTPStatus.OK.value
successful_start_response.json = lambda: json_successful_start_request

unsuccessful_start_response: Response = Response()
unsuccessful_start_response.status_code = HTTPStatus.CONFLICT.value
unsuccessful_start_response.json = lambda: json_unsuccessful_start_request


@pytest.mark.parametrize(
    "robot_id, isar_service_response, expected_status_code",
    [
        (1, successful_start_response, HTTPStatus.OK.value),
        (
            23,
            successful_start_response,
            HTTPStatus.NOT_FOUND.value,
        ),
        (
            1,
            unsuccessful_start_response,
            HTTPStatus.BAD_GATEWAY.value,
        ),
    ],
)
def test_post_start_robot(
    test_app: FastAPI,
    robot_id: int,
    isar_service_response: Response,
    expected_status_code: int,
):

    test_app.dependency_overrides[get_isar_service] = setup_isar_service_start(
        response=isar_service_response
    )
    with TestClient(test_app) as client:
        response = client.post(f"/robots/{robot_id}/start/1")
        assert response.status_code == expected_status_code


json_successful_stop_request = {
    "message": "Mission stopping",
    "stopped": True,
}

json_unsuccessful_stop_request = {
    "message": "Waiting for return message on queue timed out",
    "stopped": False,
}

successful_stop_response = Response()
successful_stop_response.status_code = HTTPStatus.OK
successful_stop_response.json = lambda: json_successful_stop_request

unsuccessful_stop_response = Response()
unsuccessful_stop_response.status_code = HTTPStatus.REQUEST_TIMEOUT.value
unsuccessful_stop_response.json = lambda: json_unsuccessful_stop_request


@pytest.mark.parametrize(
    "robot_id, isar_response, expected_status_code",
    [
        (1, successful_stop_response, HTTPStatus.OK.value),
        (
            23,
            successful_stop_response,
            HTTPStatus.NOT_FOUND.value,
        ),
        (
            1,
            unsuccessful_stop_response,
            HTTPStatus.BAD_GATEWAY.value,
        ),
    ],
)
def test_post_stop_robot(
    test_app: FastAPI,
    robot_id: int,
    isar_response: Response,
    expected_status_code: int,
):

    test_app.dependency_overrides[get_isar_service] = setup_isar_service_stop(
        isar_response
    )
    with TestClient(test_app) as client:
        response = client.post(f"/robots/{robot_id}/stop")
        assert response.status_code == expected_status_code
