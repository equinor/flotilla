import json
import os

import pytest
from flotilla_openapi.models.mission import Mission
from flotilla_openapi.models.tag import Tag

from flotilla.database.models import InspectionType
from flotilla.echo.deserializers import mission_deserializer, tag_deserializer

CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))


@pytest.mark.parametrize(
    "file, expected_return",
    [
        (
            "tag_sensor_multiple.json",
            Tag(
                tag_id="A-VB23-0111",
                inspection_types=[
                    InspectionType.image.value,
                    InspectionType.thermal_image.value,
                ],
            ),
        ),
        (
            "tag_sensor_error.json",
            Tag(
                tag_id="A-VB23-0111",
                inspection_types=[InspectionType.thermal_image.value],
            ),
        ),
        (
            "tag_sensor_none.json",
            Tag(
                tag_id="A-VB23-0111",
                inspection_types=[InspectionType.image.value],
            ),
        ),
    ],
)
def test_tag_parser(file, expected_return):
    path = f"{CURRENT_DIR}/data/{file}"
    with open(path) as f:
        data = json.load(f)
    tag: Tag = tag_deserializer(data)
    assert tag == expected_return


@pytest.mark.parametrize(
    "file, expected_return",
    [
        (
            "mission.json",
            Mission(
                id=78,
                name="TestMission",
                link="https://echo.equinor.com/mp?editId=78",
                tags=[
                    Tag(
                        tag_id="314-LD-1001",
                        inspection_types=[InspectionType.image.value],
                    ),
                    Tag(
                        tag_id="344-LD-1004",
                        inspection_types=[InspectionType.thermal_image.value],
                    ),
                ],
            ),
        ),
    ],
)
def test_mission_parser(file, expected_return):
    path = f"{CURRENT_DIR}/data/{file}"
    with open(path) as f:
        data = json.load(f)
    mission: Mission = mission_deserializer(data)
    assert mission == expected_return
