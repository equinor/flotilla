import json
import os
from typing import List

from requests import Response

from flotilla.services.echo import EchoServiceInterface, mission_deserializer

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
        self.exceptions: List[Exception] = []

    def get_missions(self) -> Response:
        response: Response = self._get_missions_request()
        return list(map(mission_deserializer, response.json()))

    def get_mission(self, mission_id: int) -> Response:
        response: Response = self._get_mission_request()
        return mission_deserializer(response.json())

    def _get_mission_request(self):
        if self.exceptions:
            raise self.exceptions.pop(0)
        if self.mission_responses:
            response: Response = self.mission_responses.pop(0)
            response.raise_for_status()
            return response
        return self.default_mission_response

    def _get_missions_request(self):
        if self.exceptions:
            raise self.exceptions.pop(0)
        if self.missions_responses:
            response: Response = self.missions_responses.pop(0)
            response.raise_for_status()
            return response

        return self.default_missions_response

    def add_missions_responses(self, responses: List[Response]):
        self.missions_responses += responses

    def add_mission_responses(self, responses: List[Response]):
        self.mission_responses += responses

    def add_exceptions(self, exceptions: List[Exception]):
        self.exceptions += exceptions


def get_echo_service_mock():
    echo_requests_mock = EchoServiceMock()
    yield echo_requests_mock
