from abc import ABCMeta, abstractmethod
from typing import List

from azure.identity import DefaultAzureCredential
from flotilla_openapi.models.mission import Mission
from flotilla_openapi.models.tag import Tag
from requests import Response

from flotilla.database.models import InspectionType
from flotilla.services import RequestHandler, get_azure_credentials
from flotilla.settings import settings

echo_sensor_to_inspection_type: dict = {
    "Picture": InspectionType.image.value,
    "ThermicPicture": InspectionType.thermal_image.value,
}


class EchoDeserializerException(Exception):
    pass


class EchoServiceInterface(metaclass=ABCMeta):
    @abstractmethod
    def get_missions(self) -> List[Mission]:
        pass

    @abstractmethod
    def get_mission(self, mission_id: int) -> Mission:
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

    def get_mission(self, mission_id: int) -> Mission:
        response: Response = self._get_mission_request(mission_id=mission_id)
        return mission_deserializer(response.json())

    def get_missions(self) -> List[Mission]:
        response: Response = self._get_missions_request()
        return list(map(mission_deserializer, response.json()))

    def _get_missions_request(self) -> Response:
        token: str = self.credentials.get_token(self.request_scope).token

        url: str = f"{self.echo_api_url}/robots/robot-plan/"
        params: dict = {
            "InstallationCode": self.installation_code,
        }
        response: Response = self.request_handler.get(
            url=url, headers={"Authorization": f"Bearer {token}"}, params=params
        )

        return response

    def _get_mission_request(self, mission_id: int) -> Response:
        token: str = self.credentials.get_token(self.request_scope).token

        url: str = f"{self.echo_api_url}/robots/robot-plan/{mission_id}"
        response: Response = self.request_handler.get(
            url=url, headers={"Authorization": f"Bearer {token}"}
        )

        return response


def mission_deserializer(json_response: dict):
    try:
        id: int = json_response["robotPlanId"]
        name: str = json_response["name"]
        link: str = f"https://echo.equinor.com/mp?editId={id}"
        tags: List[str] = list(map(tag_deserializer, json_response["planItems"]))
    except Exception as e:
        raise EchoDeserializerException from e
    return Mission(id=id, name=name, link=link, tags=tags)


def tag_deserializer(json_reponse: dict):
    try:
        tag_id: str = json_reponse["tag"]
        sensor_type: List = json_reponse["sensorTypes"]
        inspection_types: List[str] = []
        if sensor_type:
            for sensor in sensor_type:
                try:
                    inspection_types.append(
                        echo_sensor_to_inspection_type[sensor["sensorTypeKey"]]
                    )
                except KeyError:
                    continue
        else:
            inspection_types = [InspectionType.image.value]
    except Exception as e:
        raise EchoDeserializerException from e
    return Tag(tag_id=tag_id, inspection_types=inspection_types)


def get_echo_service():
    echo_requests: EchoService = EchoService()
    yield echo_requests
