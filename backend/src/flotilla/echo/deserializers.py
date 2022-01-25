from typing import List

from flotilla_openapi.models.mission import Mission
from flotilla_openapi.models.tag import Tag

from flotilla.database.models import InspectionType

echo_sensor_to_inspection_type: dict = {
    "Picture": InspectionType.image.value,
    "ThermicPicture": InspectionType.thermal_image.value,
}


def tag_deserializer(json_reponse: dict):
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
    return Tag(tag_id=tag_id, inspection_types=inspection_types)


def mission_deserializer(json_response: dict):
    id: int = json_response["robotPlanId"]
    name: str = json_response["name"]
    link: str = f"https://echo.equinor.com/mp?editId={id}"
    tags: List[str] = list(map(tag_deserializer, json_response["planItems"]))
    return Mission(id=id, name=name, link=link, tags=tags)
