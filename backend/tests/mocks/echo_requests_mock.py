import json
import os
from typing import List

from requests import HTTPError, RequestException, Response

from flotilla.services.echo.service import EchoServiceInterface

CURRENT_DIR: str = os.path.dirname(os.path.abspath(__file__))


def default_missions_response() -> Response:
    file_default_missions: str = f"{CURRENT_DIR}/data/default_missions.json"
    with open(file_default_missions) as file:
        default_missions_json: dict = json.load(file)
    response: Response = Response()
    response.status_code = 200
    response.json = lambda: default_missions_json


def default_mission_response() -> Response:
    file_default_mission: str = f"{CURRENT_DIR}/data/default_mission.json"
    with open(file_default_mission) as file:
        default_mission_json: dict = json.load(file)
    response: Response = Response()
    response.status_code = 200
    response.json = lambda: default_mission_json


class EchoServiceMock(EchoServiceInterface):
    def __init__(self) -> None:
        self.missions_responses: List[Response] = []
        self.mission_responses: List[Response] = []

        self.default_missions_response: Response = default_missions_response()
        self.default_mission_response: Response = default_mission_response()

    def get_missions(self) -> Response:
        if self.missions_responses:
            response: Response = self.missions_responses.pop(0)
            try:
                response.raise_for_status()
            except HTTPError:
                raise RequestException
            return response

        return self.default_missions_response

    def get_mission(self, mission_id: int) -> Response:
        if self.mission_responses:
            response: Response = self.mission_responses.pop(0)
            try:
                response.raise_for_status()
            except HTTPError:
                raise RequestException
            return response
        return self.default_mission_response

    def add_missions_responses(self, responses: List[Response]):
        self.missions_responses += responses

    def add_mission_responses(self, responses: List[Response]):
        self.mission_responses += responses


def get_echo_service_mock():
    echo_requests_mock = EchoServiceMock()
    yield echo_requests_mock
