from typing import List

from flotilla.database.crud import read_robots
from flotilla.database.models import RobotDBModel


def test_get_robots(session):
    robots: List[RobotDBModel] = read_robots(session)
    assert len(robots) == 2
