from http import HTTPStatus

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from requests import RequestException, Response

from flotilla.database.crud import read_by_id
from flotilla.database.models import ReportDBModel, RobotDBModel


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

start_response_ok: Response = Response()
start_response_ok.status_code = HTTPStatus.OK.value
start_response_ok.json = lambda: json_successful_start_request

start_response_conflict: Response = Response()
start_response_conflict.status_code = HTTPStatus.CONFLICT.value
start_response_conflict.json = lambda: json_unsuccessful_start_request


@pytest.mark.parametrize(
    "robot_id, isar_service_response, exception, expected_status_code",
    [
        (1, start_response_ok, None, HTTPStatus.OK.value),
        (
            23,
            start_response_ok,
            None,
            HTTPStatus.NOT_FOUND.value,
        ),
        (
            1,
            start_response_conflict,
            None,
            HTTPStatus.CONFLICT.value,
        ),
        (1, start_response_ok, RequestException, HTTPStatus.BAD_GATEWAY.value),
    ],
)
def test_post_start_robot(
    test_app: FastAPI,
    robot_id: int,
    isar_service_response: Response,
    exception: Exception,
    expected_status_code: int,
    mocker,
    session,
):
    mocker.patch("requests.request").return_value = isar_service_response
    if exception:
        mocker.patch("requests.request").side_effect = exception
    with TestClient(test_app) as client:
        response = client.post(f"/robots/{robot_id}/start/1")
        assert response.status_code == expected_status_code
        if expected_status_code == HTTPStatus.OK.value:
            report_id = response.json().get("report_id")
            assert read_by_id(ReportDBModel, db=session, item_id=report_id)


json_successful_stop_request = {
    "message": "Mission stopping",
    "stopped": True,
}

json_unsuccessful_stop_request = {
    "message": "Waiting for return message on queue timed out",
    "stopped": False,
}

stop_response_ok = Response()
stop_response_ok.status_code = HTTPStatus.OK
stop_response_ok.json = lambda: json_successful_stop_request

stop_response_timeout = Response()
stop_response_timeout.status_code = HTTPStatus.REQUEST_TIMEOUT.value
stop_response_timeout.json = lambda: json_unsuccessful_stop_request


@pytest.mark.parametrize(
    "robot_id, isar_service_response, exception, expected_status_code",
    [
        (1, stop_response_ok, None, HTTPStatus.OK.value),
        (
            23,
            stop_response_ok,
            None,
            HTTPStatus.NOT_FOUND.value,
        ),
        (
            1,
            stop_response_timeout,
            None,
            HTTPStatus.REQUEST_TIMEOUT.value,
        ),
        (1, stop_response_ok, RequestException, HTTPStatus.BAD_GATEWAY.value),
    ],
)
def test_post_stop_robot(
    test_app: FastAPI,
    robot_id: int,
    isar_service_response: Response,
    exception: Exception,
    expected_status_code: int,
    mocker,
):

    mocker.patch("requests.request").return_value = isar_service_response
    if exception:
        mocker.patch("requests.request").side_effect = exception
    with TestClient(test_app) as client:
        response = client.post(f"/robots/{robot_id}/stop")
        assert response.status_code == expected_status_code
