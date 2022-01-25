from abc import ABCMeta, abstractmethod

from azure.core.exceptions import ClientAuthenticationError
from azure.identity import DefaultAzureCredential
from requests import Response, get

from flotilla.settings import settings


def get_azure_credentials():
    try:
        return DefaultAzureCredential()
    except ClientAuthenticationError as e:
        raise e


class EchoRequestsInteface(metaclass=ABCMeta):
    @abstractmethod
    def get_missions(self) -> Response:
        pass

    @abstractmethod
    def get_mission(self, mission_id: int) -> Response:
        pass


class EchoRequests(EchoRequestsInteface):
    def __init__(
        self,
        client_id: str = settings.ECHO_CLIENT_ID,
        scope: str = settings.ECHO_APP_SCOPE,
        echo_api_url: str = settings.ECHO_API_URL,
        installation_code: str = settings.INSTALLATION_CODE,
    ) -> None:
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
        response: Response = get(
            url=url, headers={"Authorization": f"Bearer {token}"}, params=params
        )

        return response

    def get_mission(self, mission_id: int) -> Response:
        token: str = self.credentials.get_token(self.request_scope).token

        url: str = f"{self.echo_api_url}/robots/robot-plan/{mission_id}"
        response: Response = get(url=url, headers={"Authorization": f"Bearer {token}"})

        return response


def get_echo_requests():
    echo_requests: EchoRequests = EchoRequests()
    yield echo_requests
