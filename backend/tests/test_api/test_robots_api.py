import json
from http import HTTPStatus

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from requests import RequestException
from requests import Response as RequestsResponse
from sqlalchemy.orm import Session
from starlette.responses import Response

from flotilla.database.crud import read_report_by_id, read_robot_by_id
from flotilla.database.models import ReportDBModel, RobotDBModel


def test_get_robots(test_app: FastAPI):
    with TestClient(test_app) as client:
        response = client.get("/robots")
        assert response.status_code == HTTPStatus.OK.value
        assert len(response.json()) == 2


def test_create_robot_success(test_app: FastAPI, session: Session):
    body: dict = {
        "id": 0,
        "name": "test_robot",
        "model": "test_robot_model",
        "serial_number": "test_robot_serial_number",
        "host": "localhost",
        "port": 3000,
        "enabled": True,
        "status": "available",
        "capabilities": ["Image", "ThermalImage"],
    }
    with TestClient(test_app) as client:
        response: Response = client.post(url="/robots", data=json.dumps(body))
        assert response.status_code == HTTPStatus.OK.value
        assert read_robot_by_id(db=session, id=response.json()["robot_id"])


@pytest.mark.parametrize(
    "expected_status_code, body",
    [
        (
            HTTPStatus.BAD_REQUEST.value,
            {  # Capabilities contain an invalid inspection type which should cause BAD REQUEST
                "id": 0,
                "name": "test_robot",
                "model": "test_robot_model",
                "serial_number": "test_robot_serial_number",
                "host": "localhost",
                "port": 3000,
                "enabled": True,
                "status": "available",
                "capabilities": [
                    "Image",
                    "ThermalImage",
                    "this_is_an_invalid_inspection",
                ],
            },
        ),
        (
            HTTPStatus.CONFLICT.value,
            {  # Name is a robot that already exists and this should cause CONFLICT
                "id": 0,
                "name": "Harald",
                "model": "test_robot_model",
                "serial_number": "test_robot_serial_number",
                "host": "localhost",
                "port": 3000,
                "enabled": True,
                "status": "available",
                "capabilities": ["Image", "ThermalImage"],
            },
        ),
        (
            HTTPStatus.UNPROCESSABLE_ENTITY.value,
            {  # Port is not an integer which should cause a validation error, UNPROCESSABLE_ENTITY
                "id": 0,
                "name": "test_robot",
                "model": "test_robot_model",
                "serial_number": "test_robot_serial_number",
                "host": "localhost",
                "port": "This is not an integer and should cause validation error",
                "enabled": True,
                "status": "available",
                "capabilities": ["Image", "ThermalImage"],
            },
        ),
    ],
)
def test_create_robot_fails_with_invalid_input(
    test_app: FastAPI, expected_status_code: int, body: dict
):
    with TestClient(test_app) as client:
        response: Response = client.post(url="/robots", data=json.dumps(body))

        assert response.status_code == expected_status_code


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

start_response_ok: RequestsResponse = RequestsResponse()
start_response_ok.status_code = HTTPStatus.OK.value
start_response_ok.json = lambda: json_successful_start_request

start_response_conflict: RequestsResponse = RequestsResponse()
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
            assert read_report_by_id(db=session, id=report_id)


json_successful_stop_request = {
    "message": "Mission stopping",
    "stopped": True,
}

json_unsuccessful_stop_request = {
    "message": "Waiting for return message on queue timed out",
    "stopped": False,
}

stop_response_ok: RequestsResponse = RequestsResponse()
stop_response_ok.status_code = HTTPStatus.OK
stop_response_ok.json = lambda: json_successful_stop_request

stop_response_timeout: RequestsResponse = RequestsResponse()
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


@pytest.mark.parametrize("enable", [True, False])
def test_enable_robot(test_app: FastAPI, session: Session, enable: bool):
    robot_id: int = 1
    robot: RobotDBModel = (
        session.query(RobotDBModel).where(RobotDBModel.id == robot_id).first()
    )

    # Ensure the current status of enable is opposite of what we're setting so we can assert a change
    robot.enabled = not enable
    session.commit()

    with TestClient(test_app) as client:
        response: Response = client.post(
            url=f"/robots/{robot_id}/enable", params={"enable": enable}
        )

        assert response.status_code == HTTPStatus.OK.value
        assert robot.enabled == enable


def test_enable_robot_throws_not_found(test_app: FastAPI, session: Session):
    robot_id: int = 72  # Should not exist in database
    enable: bool = True
    with TestClient(test_app) as client:
        response: Response = client.post(
            url=f"/robots/{robot_id}/enable", params={"enable": enable}
        )

    assert response.status_code == HTTPStatus.NOT_FOUND.value
