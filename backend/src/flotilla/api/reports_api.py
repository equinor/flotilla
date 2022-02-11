from http import HTTPStatus
from logging import getLogger
from typing import List

from fastapi import APIRouter, Depends, Path, Response, Security
from flotilla_openapi.models.problem_details import ProblemDetails
from flotilla_openapi.models.report import Report
from pytest import Session

from flotilla.api.authentication import authentication_scheme
from flotilla.database.crud import DBException, read_report_by_id, read_reports
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
    },
    tags=["Reports"],
    summary="List all available reports on the asset",
    dependencies=[Security(authentication_scheme)],
)
async def get_reports(
    db: Session = Depends(get_db),
) -> List[Report]:
    """List all available reports on the asset."""
    db_reports: List[ReportDBModel] = read_reports(db)
    reports: List[Report] = [report.get_api_report() for report in db_reports]
    return reports


@router.get(
    "/reports/{report_id}",
    responses={
        HTTPStatus.OK.value: {
            "model": Report,
            "description": "Request successful",
        },
    },
    tags=["Reports"],
    summary="Lookup a single report",
)
async def get_report(
    response: Response,
    report_id: int = Path(None, description=""),
    db: Session = Depends(get_db),
) -> Report:
    """Lookup the report with the specified id"""
    try:
        db_report: ReportDBModel = read_report_by_id(db=db, report_id=report_id)
        report: Report = db_report.get_api_report()
    except DBException:
        logger.exception(f"Could not get report with id {report_id}.")
        response.status_code = HTTPStatus.NOT_FOUND.value
        return ProblemDetails(
            title=NOT_FOUND_DESCRIPTION, status=HTTPStatus.NOT_FOUND.value
        )
    return report
