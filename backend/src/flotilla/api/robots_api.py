from typing import List

from fastapi import APIRouter, Depends, Path, Response, Security
from flotilla_openapi.models.problem_details import ProblemDetails
from flotilla_openapi.models.robot import Robot
from pytest import Session

from flotilla.api.authentication import authentication_scheme
from flotilla.database.crud.crud import get_robot_by_id, get_robots
from flotilla.database.db import SessionLocal
from flotilla.database.models import Robot as RobotDB

router = APIRouter()


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


@router.get(
    "/robots",
    responses={
        200: {"model": List[Robot], "description": "Request successful"},
    },
    tags=["Robots"],
    summary="List all robots on the asset.",
    dependencies=[Security(authentication_scheme)],
)
async def read_robots(response: Response, db: Session = Depends(get_db)) -> List[Robot]:
    robots: List[RobotDB] = get_robots(db)
    robots_api: List[Robot] = [robot.get_api_robot() for robot in robots]
    return robots_api


@router.get(
    "/robots/{robot_id}",
    responses={
        200: {"model": Robot, "description": "Request successful"},
        404: {"model": ProblemDetails, "description": "No robot with given id exist"},
    },
    tags=["Robots"],
    summary="Lookup a single robot",
    dependencies=[Security(authentication_scheme)],
)
async def read_robot(
    response: Response,
    robot_id: int = Path(None, description=""),
    db: Session = Depends(get_db),
) -> Robot:
    robot: RobotDB = get_robot_by_id(db=db, robot_id=robot_id)
    if not robot:
        response.status_code = 404
        return ProblemDetails(title=f"No robot with id: {robot_id}", status=404)
    robot_api: Robot = robot.get_api_robot()
    return robot_api
