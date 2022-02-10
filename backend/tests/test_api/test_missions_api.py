import json
import os
from http import HTTPStatus
from typing import Optional

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from requests import RequestException, Response

from flotilla.services.echo.service import get_echo_service
from tests.mocks.echo_service_mock import EchoServiceMock

CURRENT_DIR: str = os.path.dirname(os.path.abspath(__file__))

file_missions: str = f"{CURRENT_DIR}/data/missions.json"
with open(file_missions) as file:
    missions_json: dict = json.load(file)


def setup_echo_service_read_missions(
    response: Response, exception: Optional[Exception] = None
):
    echo_service_mock = EchoServiceMock()
    echo_service_mock.add_missions_responses([response])

    if exception:
        echo_service_mock.add_exceptions(exceptions=[exception])

    def get_echo_service_mock():
        yield echo_service_mock

    return get_echo_service_mock


def setup_echo_service_read_mission(
    response: Response, exception: Optional[Exception] = None
):
    echo_service_mock = EchoServiceMock()
    echo_service_mock.add_mission_responses([response])

    if exception:
        echo_service_mock.add_exceptions(exceptions=[exception])

    def get_echo_service_mock():
        yield echo_service_mock

    return get_echo_service_mock


echo_missions_response_ok: Response = Response()
echo_missions_response_ok.status_code = HTTPStatus.OK.value
echo_missions_response_ok.json = lambda: missions_json

echo_missions_response_not_found: Response = Response()
echo_missions_response_not_found.status_code = HTTPStatus.NOT_FOUND.value
echo_missions_response_not_found.json = lambda: dict()


@pytest.mark.parametrize(
    "echo_service_response, exception, expected_status_code",
    [
        (echo_missions_response_ok, None, HTTPStatus.OK.value),
        (echo_missions_response_not_found, None, HTTPStatus.NOT_FOUND.value),
        (echo_missions_response_ok, RequestException, HTTPStatus.BAD_GATEWAY.value),
    ],
)
def test_get_missions(
    test_app: FastAPI,
    echo_service_response: Response,
    exception: Exception,
    expected_status_code: int,
):
    test_app.dependency_overrides[get_echo_service] = setup_echo_service_read_missions(
        response=echo_service_response, exception=exception
    )
    with TestClient(test_app) as client:
        response = client.get("/missions")
        assert response.status_code == expected_status_code


file_mission_error: str = f"{CURRENT_DIR}/data/mission_impossible.json"
with open(file_mission_error) as file:
    mission_error_json: dict = json.load(file)

file_mission_success: str = f"{CURRENT_DIR}/data/mission_success.json"
with open(file_mission_success) as file:
    mission_success_json: dict = json.load(file)

echo_mission_response_ok: Response = Response()
echo_mission_response_ok.status_code = HTTPStatus.OK.value
echo_mission_response_ok.json = lambda: mission_success_json

echo_mission_response_not_found: Response = Response()
echo_mission_response_not_found.status_code = HTTPStatus.NOT_FOUND.value
echo_mission_response_not_found.json = lambda: dict()

echo_mission_response_payload_error: Response = Response()
echo_mission_response_payload_error.status_code = HTTPStatus.OK.value
echo_mission_response_payload_error.json = lambda: mission_error_json


@pytest.mark.parametrize(
    " echo_service_response, exception, expected_status_code",
    [
        (echo_mission_response_ok, None, HTTPStatus.OK.value),
        (echo_mission_response_not_found, None, HTTPStatus.NOT_FOUND.value),
        (echo_mission_response_payload_error, None, HTTPStatus.BAD_GATEWAY.value),
        (echo_mission_response_ok, RequestException, HTTPStatus.BAD_GATEWAY.value),
    ],
)
def test_get_single_mission(
    test_app: FastAPI,
    echo_service_response: Response,
    exception: Exception,
    expected_status_code: int,
):
    test_app.dependency_overrides[get_echo_service] = setup_echo_service_read_mission(
        response=echo_service_response, exception=exception
    )
    with TestClient(test_app) as client:
        response = client.get(f"/missions/0")
        assert response.status_code == expected_status_code
