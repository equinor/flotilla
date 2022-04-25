from datetime import datetime, timedelta, timezone
from http import HTTPStatus
from typing import List

import pytest
from fastapi import HTTPException

from flotilla.database.crud import (
    create_event,
    create_report,
    read_by_id,
    read_by_model,
    read_event_by_id,
    read_events_by_time_overlap_and_robot_id,
    read_report_by_id,
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

valid_ids: list[int] = [1, 2]
invalid_ids: list[int] = [-1, 13, 56]


def test_read_robots(session):
    robots: List[RobotDBModel] = read_robots(db=session)
    assert len(robots) > 0


@pytest.mark.parametrize(
    "robot_id",
    valid_ids,
)
def test_read_robot_by_id(robot_id: int, session):
    robot: RobotDBModel = read_robot_by_id(db=session, id=1)
    assert robot


@pytest.mark.parametrize(
    "robot_id",
    invalid_ids,
)
def test_read_robot_by_id_throws_404(robot_id: int, session):
    with pytest.raises(HTTPException) as e:
        robot: RobotDBModel = read_robot_by_id(db=session, id=33)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


@pytest.mark.parametrize(
    "report_id",
    valid_ids,
)
def test_read_report_by_id(report_id: int, session):
    report: ReportDBModel = read_report_by_id(db=session, id=report_id)
    assert report


@pytest.mark.parametrize(
    "report_id",
    invalid_ids,
)
def test_read_report_by_id_throws_404(report_id: int, session):
    with pytest.raises(HTTPException) as e:
        report: ReportDBModel = read_report_by_id(db=session, id=report_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


@pytest.mark.parametrize(
    "event_id",
    valid_ids,
)
def test_read_event_by_id(event_id: int, session):
    event: EventDBModel = read_event_by_id(db=session, id=event_id)
    assert event


@pytest.mark.parametrize(
    "event_id",
    invalid_ids,
)
def test_read_event_by_id_throws_404(event_id: int, session):
    with pytest.raises(HTTPException) as e:
        event: EventDBModel = read_event_by_id(db=session, id=event_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


def test_create_report(session):
    robot_id: int = 1
    isar_mission_id: str = "isar_id_test"
    echo_mission_id: int = 12345
    report_status: ReportStatus = ReportStatus.in_progress
    pre_count: int = len(read_by_model(model_type=ReportDBModel, db=session))
    report_id: int = create_report(
        db=session,
        robot_id=robot_id,
        isar_mission_id=isar_mission_id,
        echo_mission_id=echo_mission_id,
        report_status=report_status,
    )
    post_count: int = len(read_by_model(model_type=ReportDBModel, db=session))
    assert pre_count + 1 == post_count
    assert read_report_by_id(db=session, id=report_id)


def test_create_event(session):
    robot_id: int = 1
    echo_mission_id: int = 12345
    start_time = datetime.now()
    pre_count: int = len(read_by_model(model_type=EventDBModel, db=session))
    event_id: int = create_event(
        db=session,
        robot_id=robot_id,
        echo_mission_id=echo_mission_id,
        start_time=start_time,
        estimated_duration=timedelta(hours=1),
    )
    post_count: int = len(read_by_model(model_type=EventDBModel, db=session))
    assert pre_count + 1 == post_count
    assert read_event_by_id(db=session, id=event_id)


def test_remove_event(session):
    event_id: int = 1
    assert read_event_by_id(db=session, id=event_id)
    remove_event(db=session, event_id=event_id)
    with pytest.raises(HTTPException) as e:
        read_event_by_id(db=session, id=event_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


@pytest.mark.parametrize(
    "start_time, duration, expected_len",
    [
        (datetime.now(tz=timezone.utc) - timedelta(hours=2), timedelta(hours=1.5), 1),
        (
            datetime.now(tz=timezone.utc) - timedelta(days=1),
            timedelta(hours=1),
            0,
        ),
        (
            datetime.now(tz=timezone.utc) - timedelta(hours=2.5),
            timedelta(hours=1),
            1,
        ),
        (
            datetime.now(tz=timezone.utc) + timedelta(hours=1.5),
            timedelta(hours=1),
            0,
        ),
    ],
)
def test_read_event_by_robot_id_and_time_span(
    start_time: datetime, duration: timedelta, expected_len: int, session
):
    end_time = start_time + duration
    events: List[EventDBModel] = read_events_by_time_overlap_and_robot_id(
        start_time=start_time, end_time=end_time, robot_id=1, db=session
    )
    assert len(events) == expected_len
