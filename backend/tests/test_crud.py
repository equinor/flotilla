import datetime
from http import HTTPStatus
from typing import List

import pytest
from fastapi import HTTPException

from flotilla.database.crud import (
    create_event,
    create_report,
    read_event_by_id,
    read_events,
    read_report_by_id,
    read_reports,
    read_robot_by_id,
    read_robots,
    remove_event,
)
from flotilla.database.models import (
    EventDBModel,
    ReportDBModel,
    ReportStatus,
    RobotDBModel,
)

good_ids: list[int] = [1, 2]
bad_ids: list[int] = [-1, 13, 56]


def test_read_robots(session):
    robots: List[RobotDBModel] = read_robots(session)
    assert len(robots) > 0


@pytest.mark.parametrize(
    "robot_id",
    good_ids,
)
def test_read_robot_by_id(robot_id: int, session):
    robot: RobotDBModel = read_robot_by_id(db=session, robot_id=robot_id)
    assert robot


@pytest.mark.parametrize(
    "robot_id",
    bad_ids,
)
def test_read_robot_by_id_throws_404(robot_id: int, session):
    with pytest.raises(HTTPException) as e:
        robot: RobotDBModel = read_robot_by_id(db=session, robot_id=robot_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


def test_read_reports(session):
    reports: List[ReportDBModel] = read_reports(session)
    assert len(reports) > 0


@pytest.mark.parametrize(
    "report_id",
    good_ids,
)
def test_read_report_by_id(report_id: int, session):
    report: ReportDBModel = read_report_by_id(db=session, report_id=report_id)
    assert report


@pytest.mark.parametrize(
    "report_id",
    bad_ids,
)
def test_read_report_by_id_throws_404(report_id: int, session):
    with pytest.raises(HTTPException) as e:
        report: ReportDBModel = read_report_by_id(db=session, report_id=report_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


def test_read_events(session):
    events: List[EventDBModel] = read_events(session)
    assert len(events) > 0


@pytest.mark.parametrize(
    "event_id",
    good_ids,
)
def test_read_event_by_id(event_id: int, session):
    event: EventDBModel = read_event_by_id(db=session, event_id=event_id)
    assert event


@pytest.mark.parametrize(
    "event_id",
    bad_ids,
)
def test_read_event_by_id_throws_404(event_id: int, session):
    with pytest.raises(HTTPException) as e:
        event: EventDBModel = read_event_by_id(db=session, event_id=event_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


def test_create_report(session):
    robot_id = 1
    isar_mission_id: str = "isar_id_test"
    echo_mission_id: int = 12345
    report_status: ReportStatus = ReportStatus.in_progress
    pre_count: int = len(read_reports(session))
    report_id: int = create_report(
        db=session,
        robot_id=robot_id,
        isar_mission_id=isar_mission_id,
        echo_mission_id=echo_mission_id,
        report_status=report_status,
    )
    post_count = len(read_reports(session))
    assert pre_count + 1 == post_count
    assert read_report_by_id(db=session, report_id=report_id)


def test_create_event(session):
    robot_id: int = 1
    echo_mission_id: int = 12345
    start_time = datetime.datetime.now()
    pre_count: int = len(read_events(session))
    event_id: int = create_event(
        db=session,
        robot_id=robot_id,
        echo_mission_id=echo_mission_id,
        start_time=start_time,
    )
    post_count = len(read_events(session))
    assert pre_count + 1 == post_count
    assert read_event_by_id(db=session, event_id=event_id)


def test_remove_event(session):
    event_id: int = 1
    assert read_event_by_id(db=session, event_id=event_id)
    remove_event(db=session, event_id=event_id)
    with pytest.raises(HTTPException) as e:
        read_event_by_id(db=session, event_id=event_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value
