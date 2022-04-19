from http import HTTPStatus
from logging import getLogger
from typing import List

from fastapi import (
    APIRouter,
    Body,
    Depends,
    HTTPException,
    Path,
    Query,
    Response,
    Security,
)
from flotilla_openapi.models.create_robot_response import CreateRobotResponse
from flotilla_openapi.models.error import Error
from flotilla_openapi.models.post_response import PostResponse
from flotilla_openapi.models.robot import Robot
from flotilla_openapi.models.start_response import StartResponse
from flotilla_openapi.models.success import Success
from requests import Response as RequestResponse
from sqlalchemy.orm import Session

from flotilla.api.authentication import authentication_scheme
from flotilla.api.pagination import PaginationParams
from flotilla.database.crud import (
    create_report,
    create_robot,
    read_by_id,
    read_list_paginated,
)
from flotilla.database.db import get_db
from flotilla.database.models import ReportStatus, RobotDBModel
from flotilla.services.isar import IsarService, get_isar_service

logger = getLogger("api")

router = APIRouter()

BAD_GATEWAY_DESCRIPTION = "Failed to stop - Error while contacting ISAR"
NOT_FOUND_DESCRIPTION = "Not Found - No robot with given id"


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
async def get_robots(
    db: Session = Depends(get_db),
    pagination_params: PaginationParams = Depends(),
) -> List[Robot]:
    db_robots: List[RobotDBModel] = read_list_paginated(
        db=db, params=pagination_params, modelType=RobotDBModel
    )
    robots: List[Robot] = [robot.get_api_robot() for robot in db_robots]
    return robots


@router.post(
    "/robots",
    responses={
        HTTPStatus.OK.value: {
            "model": CreateRobotResponse,
            "description": "Successfully added robot to database",
        },
        HTTPStatus.UNAUTHORIZED.value: {
            "model": Error,
            "description": "Unauthorized",
        },
    },
    tags=["Robots"],
    summary="Create a new robot",
    dependencies=[Security(authentication_scheme)],
)
async def post_robot(
    db: Session = Depends(get_db),
    robot: Robot = Body(None, description="New robot to be added"),
) -> CreateRobotResponse:
    try:
        robot_id: int = create_robot(
            db=db,
            name=robot.name,
            model=robot.model,
            serial_number=robot.serial_number,
            host=robot.host,
            port=robot.port,
            enabled=robot.enabled,
            capabilities=robot.capabilities,
        )
    except HTTPException as e:
        logger.exception(f"An error occurred while creating a robot: {e.detail}")
        raise

    return CreateRobotResponse(robot_id=robot_id)


@router.get(
    "/robots/{robot_id}",
    responses={
        HTTPStatus.OK.value: {
            "model": Robot,
            "description": "Request successful",
        },
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
    try:
        db_robot: RobotDBModel = read_by_id(
            db=db, modelType=RobotDBModel, item_id=robot_id
        )
        robot: Robot = db_robot.get_api_robot()
    except HTTPException as e:
        logger.error(f"Could not get robot with id {robot_id}: {e.detail}")
        raise
    return robot


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
    try:
        robot: RobotDBModel = read_by_id(RobotDBModel, db, robot_id)
        response_isar: RequestResponse = isar_service.start_mission(
            host=robot.host, port=robot.port, mission_id=mission_id
        )
        response_isar_json: dict = response_isar.json()
        report_id: int = create_report(
            db,
            robot_id=robot_id,
            isar_mission_id=response_isar_json["mission_id"],
            echo_mission_id=mission_id,
            report_status=ReportStatus.in_progress,
        )
    except HTTPException as e:
        logger.error(
            f"Could not start mission with id {mission_id} for robot with id {robot_id}: {e.detail}"
        )
        raise

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
    try:
        robot: RobotDBModel = read_by_id(RobotDBModel, db, robot_id)
        response_isar: RequestResponse = isar_service.stop(
            host=robot.host, port=robot.port
        )
    except HTTPException as e:
        logger.error(f"Failed to stop robot with id {robot_id}: {e.detail}")
        raise

    response_isar_json: dict = response_isar.json()
    return PostResponse(status=response_isar_json["message"])


@router.post(
    "/robots/{robot_id}/enable",
    responses={
        HTTPStatus.OK.value: {
            "model": Success,
            "description": "Success",
        },
        HTTPStatus.UNAUTHORIZED.value: {
            "model": Error,
            "description": "Unauthorized",
        },
        HTTPStatus.NOT_FOUND.value: {
            "model": Error,
            "description": "Not Found",
        },
    },
    tags=["Robots"],
    summary="Enable or disable an existing robot",
    dependencies=[Security(authentication_scheme)],
)
async def post_enable_robot(
    robot_id: int = Path(None, description=""),
    enable: bool = Query(None, description=""),
    db: Session = Depends(get_db),
):
    robot: RobotDBModel = read_by_id(modelType=RobotDBModel, db=db, item_id=robot_id)
    robot.enabled = enable

    db.commit()

    return Success(detail="Success")
