from abc import ABCMeta, abstractmethod

from azure.identity import DefaultAzureCredential
from requests import Response

from flotilla.services import RequestHandler, get_azure_credentials
from flotilla.settings import settings


class EchoServiceInterface(metaclass=ABCMeta):
    @abstractmethod
    def get_missions(self) -> Response:
        pass

    @abstractmethod
    def get_mission(self, mission_id: int) -> Response:
        pass


class EchoService(EchoServiceInterface):
    def __init__(
        self,
        request_handler: RequestHandler = RequestHandler(),
        client_id: str = settings.ECHO_CLIENT_ID,
        scope: str = settings.ECHO_APP_SCOPE,
        echo_api_url: str = settings.ECHO_API_URL,
        installation_code: str = settings.INSTALLATION_CODE,
    ) -> None:
        self.request_handler: RequestHandler = request_handler
        self.client_id: str = client_id
        self.scope: str = scope
        self.request_scope: str = f"{client_id}/{scope}"
        self.echo_api_url: str = echo_api_url
        self.credentials: DefaultAzureCredential = get_azure_credentials()
        self.installation_code: str = installation_code

    def get_missions(self) -> Response:
        token: str = self.credentials.get_token(self.request_scope).token

        url: str = f"{self.echo_api_url}/robots/robot-plan/"
        params: dict = {
            "InstallationCode": self.installation_code,
        }
        response: Response = self.request_handler.get(
            url=url, headers={"Authorization": f"Bearer {token}"}, params=params
        )

        return response

    def get_mission(self, mission_id: int) -> Response:
        token: str = self.credentials.get_token(self.request_scope).token

        url: str = f"{self.echo_api_url}/robots/robot-plan/{mission_id}"
        response: Response = self.request_handler.get(
            url=url, headers={"Authorization": f"Bearer {token}"}
        )

        return response


def get_echo_service():
    echo_requests: EchoService = EchoService()
    yield echo_requests
