from http import HTTPStatus
from typing import List

from fastapi import APIRouter, Depends, Path, Response, Security
from flotilla_openapi.models.mission import Mission
from flotilla_openapi.models.problem_details import ProblemDetails
from requests import RequestException
from requests import Response as RequestResponse

from flotilla.api.authentication import authentication_scheme
from flotilla.services.echo.deserializers import mission_deserializer
from flotilla.services.echo.service import EchoService, get_echo_service

router = APIRouter()


@router.get(
    "/missions",
    responses={
        HTTPStatus.OK.value: {
            "model": List[Mission],
            "description": "Request successful",
        },
    },
    tags=["Missions"],
    summary="List all available missions on the asset",
    dependencies=[Security(authentication_scheme)],
)
async def read_missions(
    response: Response, echo_requests: EchoService = Depends(get_echo_service)
) -> List[Mission]:
    """### Overview List all available missions on the asset in the Echo mission planner"""
    try:
        echo_response: RequestResponse = echo_requests.get_missions()
    except RequestException:
        response.status_code = HTTPStatus.BAD_GATEWAY.value
        return ProblemDetails(
            title="Not found - Could not contact echo",
            status=HTTPStatus.BAD_GATEWAY.value,
        )
    missions: List[Mission] = []
    for mission in echo_response.json():
        try:
            missions.append(mission_deserializer(mission))
        except Exception:
            continue
    return missions


@router.get(
    "/missions/{mission_id}",
    responses={
        HTTPStatus.OK.value: {"model": Mission, "description": "Request successful"},
    },
    tags=["Missions"],
    summary="Lookup a single mission on the asset",
    dependencies=[Security(authentication_scheme)],
)
async def read_single_mission(
    response: Response,
    mission_id: int = Path(None, description=""),
    echo_requests: EchoService = Depends(get_echo_service),
) -> Mission:
    """### Overview Lookup a single mission on the asset"""
    try:
        echo_response: RequestResponse = echo_requests.get_mission(mission_id)
    except RequestException:
        response.status_code = HTTPStatus.BAD_GATEWAY.value
        return ProblemDetails(
            title="Not found - Could not contact echo",
            status=HTTPStatus.BAD_GATEWAY.value,
        )
    try:
        mission: Mission = mission_deserializer(echo_response.json())
    except Exception:
        response.status_code = HTTPStatus.NOT_FOUND.value
        return ProblemDetails(
            title="Could not decode response from echo",
            status=HTTPStatus.NOT_FOUND.value,
        )
    return mission
