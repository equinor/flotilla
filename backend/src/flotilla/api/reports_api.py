from http import HTTPStatus
from logging import getLogger
from typing import List

from fastapi import APIRouter, Depends, HTTPException, Path, Security
from flotilla_openapi.models.error import Error
from flotilla_openapi.models.report import Report
from pytest import Session

from flotilla.api.authentication import authentication_scheme
from flotilla.api.pagination import PaginationParams
from flotilla.database.crud import read_by_id, read_list_paginated
from flotilla.database.db import get_db
from flotilla.database.models import ReportDBModel

router = APIRouter()

logger = getLogger("api")

NOT_FOUND_DESCRIPTION = "Not Found - No report with given id"


@router.get(
    "/reports",
    responses={
        HTTPStatus.OK.value: {
            "model": List[Report],
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
    tags=["Reports"],
    summary="List all available reports on the asset",
    description="""### Overview 
    List all available reports on the asset. `start_time` defaults to current time - 7 
    days and `end_time` defaults to current time. If no value for `robot_id` is provided, reports for all robots on 
    the asset is provided.""",
    dependencies=[Security(authentication_scheme)],
)
async def get_reports(
    db: Session = Depends(get_db),
    params: PaginationParams = Depends(),
) -> List[Report]:
    """List all available reports on the asset."""
    db_reports: List[ReportDBModel] = read_list_paginated(ReportDBModel, db, params)
    reports: List[Report] = [report.get_api_report() for report in db_reports]
    return reports


@router.get(
    "/reports/{report_id}",
    responses={
        HTTPStatus.OK.value: {
            "model": Report,
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
    tags=["Reports"],
    summary="Lookup a single report",
    description="""### Overview
    Lookup the report with the specified id.""",
)
async def get_report(
    report_id: int = Path(None, description=""),
    db: Session = Depends(get_db),
) -> Report:
    """Lookup the report with the specified id"""
    try:
        db_report: ReportDBModel = read_by_id(ReportDBModel, db=db, item_id=report_id)
        report: Report = db_report.get_api_report()
    except HTTPException as e:
        logger.error(f"Failed to get report with id {report_id}: {e.detail}")
        raise
    return report
