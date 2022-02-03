from http import HTTPStatus
from typing import List

from fastapi import APIRouter, Depends, Path, Response, Security
from flotilla_openapi.models.post_response import PostResponse
from flotilla_openapi.models.problem_details import ProblemDetails
from flotilla_openapi.models.robot import Robot
from flotilla_openapi.models.start_response import StartResponse
from pytest import Session
from requests import RequestException
from requests import Response as RequestResponse

from flotilla.api.authentication import authentication_scheme
from flotilla.database.crud.crud import create_report, read_robot_by_id, read_robots
from flotilla.database.db import SessionLocal
from flotilla.database.models import ReportStatus
from flotilla.database.models import Robot as DBRobot
from flotilla.services.isar import IsarService, get_isar_service

router = APIRouter()

BAD_GATEWAY_DESCRIPTION = "Failed to stop - Error while contacting ISAR"
NOT_FOUND_DESCRIPTION = "Not Found - No robot with given id"


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


@router.get(
    "/robots",
    responses={
        HTTPStatus.OK.value: {
            "model": List[Robot],
            "description": "Request successful",
        },
    },
    tags=["Robots"],
    summary="List all robots on the asset.",
    dependencies=[Security(authentication_scheme)],
)
async def get_robots(db: Session = Depends(get_db)) -> List[Robot]:
    robots: List[DBRobot] = read_robots(db)
    robots_api: List[Robot] = [robot.get_api_robot() for robot in robots]
    return robots_api


@router.get(
    "/robots/{robot_id}",
    responses={
        HTTPStatus.OK.value: {"model": Robot, "description": "Request successful"},
    },
    tags=["Robots"],
    summary="Lookup a single robot",
    dependencies=[Security(authentication_scheme)],
)
async def get_robot(
    response: Response,
    robot_id: int = Path(None, description=""),
    db: Session = Depends(get_db),
) -> Robot:
    robot: DBRobot = read_robot_by_id(db=db, robot_id=robot_id)
    if not robot:
        response.status_code = HTTPStatus.NOT_FOUND.value
        return ProblemDetails(
            title=NOT_FOUND_DESCRIPTION, status=HTTPStatus.NOT_FOUND.value
        )
    robot_api: Robot = robot.get_api_robot()
    return robot_api


@router.post(
    "/robots/{robot_id}/start/{mission_id}",
    responses={
        HTTPStatus.OK.value: {
            "model": StartResponse,
            "description": "Mission successfully started",
        },
    },
    tags=["Robots"],
    summary="Start a mission with robot",
    dependencies=[Security(authentication_scheme)],
)
async def post_start_robot(
    response: Response,
    robot_id: int = Path(None, description=""),
    mission_id: int = Path(None, description=""),
    db: Session = Depends(get_db),
    isar_service: IsarService = Depends(get_isar_service),
) -> StartResponse:
    """Start a mission with given id using robot with robot id."""
    robot: DBRobot = read_robot_by_id(db, robot_id)
    if not robot:
        response.status_code = HTTPStatus.NOT_FOUND.value
        return ProblemDetails(title=NOT_FOUND_DESCRIPTION)

    try:
        response_isar: RequestResponse = isar_service.start_mission(
            host=robot.host, port=robot.port, mission_id=mission_id
        )
    except RequestException:
        response.status_code = HTTPStatus.BAD_GATEWAY.value
        return ProblemDetails(title=BAD_GATEWAY_DESCRIPTION)

    response_isar_json: dict = response_isar.json()
    report_id: int = create_report(
        db,
        robot_id=robot_id,
        isar_mission_id=response_isar_json["mission_id"],
        echo_mission_id=mission_id,
        report_status=ReportStatus.in_progress,
    )
    return StartResponse(status="started", report_id=report_id)


@router.post(
    "/robots/{robot_id}/stop",
    responses={
        HTTPStatus.OK.value: {
            "model": PostResponse,
            "description": "Robot successfully stopped",
        },
    },
    tags=["Robots"],
    summary="Stop robot",
    dependencies=[Security(authentication_scheme)],
)
async def post_stop_robot(
    response: Response,
    robot_id: int = Path(None, description=""),
    db: Session = Depends(get_db),
    isar_service: IsarService = Depends(get_isar_service),
) -> PostResponse:
    """Stop the execution of the current active mission. If there is no active mission on robot, nothing happens."""
    robot: DBRobot = read_robot_by_id(db, robot_id)
    if not robot:
        response.status_code = HTTPStatus.NOT_FOUND.value
        return ProblemDetails(title=NOT_FOUND_DESCRIPTION)
    try:
        response_isar: RequestResponse = isar_service.stop(
            host=robot.host, port=robot.port
        )
    except RequestException:
        response.status_code = HTTPStatus.BAD_GATEWAY.value
        return ProblemDetails(title=BAD_GATEWAY_DESCRIPTION)

    response_isar_json: dict = response_isar.json()
    return PostResponse(status=response_isar_json["message"])
