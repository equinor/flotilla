from operator import le
from typing import List

from flotilla.database.crud.crud import get_robots
from flotilla.database.models import Robot


def test_get_robots(session):
    robots: List[Robot] = get_robots(session)
    assert len(robots) == 2
