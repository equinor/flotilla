from typing import List

from fastapi import APIRouter, Depends, Path, Response, Security
from flotilla_openapi.models.mission import Mission
from flotilla_openapi.models.problem_details import ProblemDetails
from requests import RequestException
from requests import Response as RequestResponse

from flotilla.api.authentication import authentication_scheme
from flotilla.echo.deserializers import mission_deserializer
from flotilla.echo.requests import EchoRequests, get_echo_requests

router = APIRouter()


@router.get(
    "/missions",
    responses={
        200: {"model": List[Mission], "description": "Request successful"},
        404: {
            "model": ProblemDetails,
            "description": "Not found - No missions available on the asset",
        },
    },
    tags=["Missions"],
    summary="List all available missions on the asset",
    dependencies=[Security(authentication_scheme)],
)
async def read_missions(
    response: Response, echo_requests: EchoRequests = Depends(get_echo_requests)
) -> List[Mission]:
    """### Overview List all available missions on the asset in the Echo mission planner"""
    try:
        echo_response: RequestResponse = echo_requests.get_missions()
    except RequestException:
        response.status_code = 404
        return ProblemDetails(title="Not found - Could not contact echo", status=404)

    if not echo_response.status_code == 200:
        response.status_code = 404
        return ProblemDetails(title="Not found - Could not contact echo", status=404)
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
        200: {"model": Mission, "description": "Request successful"},
        404: {
            "model": ProblemDetails,
            "description": "Not found - The requested mission object does not exist",
        },
    },
    tags=["Missions"],
    summary="Lookup a single mission on the asset",
    dependencies=[Security(authentication_scheme)],
)
async def read_single_mission(
    response: Response,
    mission_id: int = Path(None, description=""),
    echo_requests: EchoRequests = Depends(get_echo_requests),
) -> Mission:
    """### Overview Lookup a single mission on the asset"""
    try:
        echo_response: RequestResponse = echo_requests.get_mission(mission_id)
    except RequestException:
        response.status_code = 404
        return ProblemDetails(title="Not found - Could not contact echo", status=404)

    if not echo_response.status_code == 200:
        response.status_code = 404
        return ProblemDetails(title="Not found - Could not contact echo", status=404)
    try:
        mission: Mission = mission_deserializer(echo_response.json())
    except Exception:
        response.status_code = 404
        return ProblemDetails(title="Could not decode response from echo", status=404)
    return mission
