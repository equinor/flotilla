from http import HTTPStatus
from logging import getLogger
from typing import List

from fastapi import APIRouter, Depends, HTTPException, Path, Security
from flotilla_openapi.models.error import Error
from flotilla_openapi.models.mission import Mission

from flotilla.api.authentication import authentication_scheme
from flotilla.services.echo import (
    EchoDeserializerException,
    EchoService,
    get_echo_service,
)

logger = getLogger("api")

router = APIRouter()


@router.get(
    "/missions",
    responses={
        HTTPStatus.OK.value: {
            "model": List[Mission],
            "description": "Request successful",
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
    tags=["Missions"],
    summary="List all available missions on the asset",
    description="""### Overview
    List all available missions on the asset in the Echo mission planner.""",
    dependencies=[Security(authentication_scheme)],
)
async def get_missions(
    echo_service: EchoService = Depends(get_echo_service),
) -> List[Mission]:
    """### Overview List all available missions on the asset in the Echo mission planner"""
    try:
        missions: List[Mission] = echo_service.get_missions()
    except HTTPException as e:
        logger.error(f"Could not get missions from echo: {e.detail}")
        raise
    except EchoDeserializerException:
        logger.error("Could not deserialize the response from Echo")
        raise HTTPException(
            status_code=HTTPStatus.BAD_GATEWAY.value,
            detail="Bad Gateway - Could not deserialize response from Echo",
        )
    return missions


@router.get(
    "/missions/{mission_id}",
    responses={
        HTTPStatus.OK.value: {"model": Mission, "description": "Request successful"},
        HTTPStatus.UNAUTHORIZED.value: {
            "model": Error,
            "description": "Unauthorized",
        },
        HTTPStatus.NOT_FOUND.value: {
            "model": Error,
            "description": "Not Found",
        },
    },
    tags=["Missions"],
    summary="Lookup a single mission on the asset",
    description="""### Overview
    Lookup a single mission on the asset.""",
    dependencies=[Security(authentication_scheme)],
)
async def get_single_mission(
    mission_id: int = Path(None, description=""),
    echo_service: EchoService = Depends(get_echo_service),
) -> Mission:
    """### Overview Lookup a single mission on the asset"""
    try:
        mission: Mission = echo_service.get_mission(mission_id)
    except HTTPException as e:
        logger.error(f"Failed to get mission with id {mission_id}: {e.detail}")
        raise
    except EchoDeserializerException:
        logger.error(f"Could not deserialize mission with id {mission_id}.")
        raise HTTPException(
            status_code=HTTPStatus.BAD_GATEWAY.value,
            detail="Bad Gateway - Could not deserialize response from Echo",
        )
    return mission
