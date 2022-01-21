from typing import List

from markupsafe import re
from sqlalchemy.orm import Session

from flotilla.database.models import Robot


def get_robots(db: Session) -> List[Robot]:
    return db.query(Robot).all()


def get_robot_by_id(db: Session, robot_id: int) -> Robot:
    return db.query(Robot).filter(Robot.id == robot_id).first()
