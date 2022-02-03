from typing import List

from flotilla.database.crud.crud import read_robots
from flotilla.database.models import Robot


def test_get_robots(session):
    robots: List[Robot] = read_robots(session)
    assert len(robots) == 2
