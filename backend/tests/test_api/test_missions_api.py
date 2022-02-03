import json
import os
from http import HTTPStatus

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from requests import Response

from flotilla.services.echo.service import get_echo_service
from tests.mocks.echo_requests_mock import EchoServiceMock

CURRENT_DIR: str = os.path.dirname(os.path.abspath(__file__))

file_missions: str = f"{CURRENT_DIR}/data/missions.json"
with open(file_missions) as file:
    missions_json: dict = json.load(file)


@pytest.mark.parametrize(
    "request_status_code, request_json, expected_status_code",
    [
        (HTTPStatus.OK.value, missions_json, HTTPStatus.OK.value),
        (HTTPStatus.NOT_FOUND.value, dict(), HTTPStatus.BAD_GATEWAY.value),
    ],
)
def test_read_missions(
    test_app: FastAPI,
    request_status_code: int,
    request_json: dict,
    expected_status_code: int,
):
    mock_response = Response()
    mock_response.status_code = request_status_code
    mock_response.json = lambda: request_json
    echo_requests_mock = EchoServiceMock()
    echo_requests_mock.add_missions_responses([mock_response])

    def get_echo_requests_mock():
        yield echo_requests_mock

    test_app.dependency_overrides[get_echo_service] = get_echo_requests_mock
    with TestClient(test_app) as client:
        response = client.get("/missions")
        assert response.status_code == expected_status_code


file_mission_error: str = f"{CURRENT_DIR}/data/mission_impossible.json"
with open(file_mission_error) as file:
    mission_error_json: dict = json.load(file)

file_mission_success: str = f"{CURRENT_DIR}/data/mission_success.json"
with open(file_mission_success) as file:
    mission_success_json: dict = json.load(file)


@pytest.mark.parametrize(
    "mission_id, request_status_code, request_json, expected_status_code",
    [
        (84, HTTPStatus.OK.value, mission_success_json, HTTPStatus.OK.value),
        (666, HTTPStatus.NOT_FOUND.value, dict(), HTTPStatus.BAD_GATEWAY.value),
        (54, HTTPStatus.OK.value, mission_error_json, HTTPStatus.NOT_FOUND.value),
    ],
)
def test_read_single_mission(
    test_app: FastAPI,
    mission_id: int,
    request_status_code: int,
    request_json: dict,
    expected_status_code: int,
):
    mock_response = Response()
    mock_response.status_code = request_status_code
    mock_response.json = lambda: request_json
    echo_requests_mock = EchoServiceMock()
    echo_requests_mock.add_mission_responses([mock_response])

    def get_echo_requests_mock():
        yield echo_requests_mock

    test_app.dependency_overrides[get_echo_service] = get_echo_requests_mock
    with TestClient(test_app) as client:
        response = client.get(f"/missions/{mission_id}")
        assert response.status_code == expected_status_code
