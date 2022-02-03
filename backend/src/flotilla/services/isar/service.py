from abc import ABCMeta, abstractmethod

from azure.identity import DefaultAzureCredential
from requests import Response

from flotilla.services import RequestHandler, get_azure_credentials
from flotilla.settings import settings


class IsarServiceInterface(metaclass=ABCMeta):
    @abstractmethod
    def start_mission(self, host: str, port: int, mission_id: int) -> Response:
        pass

    @abstractmethod
    def stop(self, host: str, port: int) -> Response:
        pass


class IsarService(IsarServiceInterface):
    def __init__(
        self,
        request_handler: RequestHandler = RequestHandler(),
        client_id: str = settings.ISAR_CLIENT_ID,
        scope: str = settings.ISAR_APP_SCOPE,
    ) -> None:
        self.request_handler: RequestHandler = request_handler
        self.client_id: str = client_id
        self.scope: str = scope
        self.request_scope: str = f"{client_id}/{scope}"
        self.credentials: DefaultAzureCredential = get_azure_credentials()

    def start_mission(self, host: str, port: int, mission_id: int) -> Response:
        token: str = self.credentials.get_token(self.request_scope).token
        url: str = f"http://{host}:{port}/schedule/start-mission"
        params: dict = {"ID": mission_id}
        response: Response = self.request_handler.post(
            url=url, headers={"Authorization": f"Bearer {token}"}, params=params
        )
        return response

    def stop(self, host: str, port: int) -> Response:
        token: str = self.credentials.get_token(self.request_scope).token
        url: str = f"http://{host}:{port}/schedule/stop-mission"
        response: Response = self.request_handler.post(
            url=url, headers={"Authorization": f"Bearer {token}"}
        )
        return response


def get_isar_service():
    isar_requests: IsarService = IsarService()
    yield isar_requests
