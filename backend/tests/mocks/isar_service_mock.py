from typing import List

from requests import Response

from flotilla.services.isar import IsarServiceInterface


def default_start_mission_response() -> Response:
    response: Response = Response()
    response.status_code = 200

    default_json = {
        "message": "Mission started",
        "started": True,
        "mission_id": "KAAWILLIAM03022022133849308",
    }
    response.json = lambda: default_json
    return response


def default_stop_response() -> Response:
    response: Response = Response()
    response.status_code = 200

    default_json = {"message": "Mission stopping", "stopped": True}
    response.json = lambda: default_json
    return response


class IsarServiceMock(IsarServiceInterface):
    def __init__(self) -> None:
        self.start_mission_responses: List[Response] = []
        self.stop_responses: List[Response] = []

        self.default_start_mission_response: Response = default_start_mission_response()
        self.default_stop_response: Response = default_stop_response()
        self.exceptions: List[Exception] = []

    def start_mission(self, host: str, port: int, mission_id: int) -> Response:
        if self.exceptions:
            raise self.exceptions.pop(0)
        if self.start_mission_responses:
            response: Response = self.start_mission_responses.pop(0)
            response.raise_for_status()
        return self.default_start_mission_response

    def stop(self, host: str, port: int) -> Response:
        if self.exceptions:
            raise self.exceptions.pop(0)
        if self.stop_responses:
            response: Response = self.stop_responses.pop(0)
            response.raise_for_status()
        return self.default_stop_response

    def add_start_mission_responses(self, responses: List[Response]) -> None:
        self.start_mission_responses += responses

    def add_stop_responses(self, responses: List[Response]) -> None:
        self.stop_responses += responses

    def add_exceptions(self, exceptions: List[Exception]) -> None:
        self.exceptions += exceptions


def get_isar_service_mock():
    isar_requests_mock = IsarServiceMock()
    yield isar_requests_mock
