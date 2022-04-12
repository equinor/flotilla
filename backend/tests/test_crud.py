from datetime import datetime, timedelta, timezone
from http import HTTPStatus
from typing import List

import pytest
from fastapi import HTTPException

from flotilla.database.crud import (
    create_event,
    create_report,
    get_by_id,
    get_list_paginated,
    read_events_by_robot_id_and_time_span,
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
    robots: List[RobotDBModel] = get_list_paginated(RobotDBModel, session)
    assert len(robots) > 0


@pytest.mark.parametrize(
    "robot_id",
    valid_ids,
)
def test_read_robot_by_id(robot_id: int, session):
    robot: RobotDBModel = get_by_id(RobotDBModel, db=session, item_id=robot_id)
    assert robot


@pytest.mark.parametrize(
    "robot_id",
    invalid_ids,
)
def test_read_robot_by_id_throws_404(robot_id: int, session):
    with pytest.raises(HTTPException) as e:
        robot: RobotDBModel = get_by_id(RobotDBModel, db=session, item_id=robot_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


def test_read_reports(session):
    reports: List[ReportDBModel] = get_list_paginated(ReportDBModel, session)
    assert len(reports) > 0


@pytest.mark.parametrize(
    "report_id",
    valid_ids,
)
def test_read_report_by_id(report_id: int, session):
    report: ReportDBModel = get_by_id(ReportDBModel, db=session, item_id=report_id)
    assert report


@pytest.mark.parametrize(
    "report_id",
    invalid_ids,
)
def test_read_report_by_id_throws_404(report_id: int, session):
    with pytest.raises(HTTPException) as e:
        report: ReportDBModel = get_by_id(ReportDBModel, db=session, item_id=report_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


def test_read_events(session):
    events: List[EventDBModel] = get_list_paginated(EventDBModel, session)
    assert len(events) > 0


@pytest.mark.parametrize(
    "event_id",
    valid_ids,
)
def test_read_event_by_id(event_id: int, session):
    event: EventDBModel = get_by_id(EventDBModel, db=session, item_id=event_id)
    assert event


@pytest.mark.parametrize(
    "start_time, duration, expected_len",
    [
        (datetime.now(tz=timezone.utc), timedelta(hours=1.5), 1),
        (
            datetime.now(tz=timezone.utc) - timedelta(days=1),
            timedelta(hours=1),
            0,
        ),
        (
            datetime.now(tz=timezone.utc) - timedelta(hours=0.5),
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
    events: List[EventDBModel] = read_events_by_robot_id_and_time_span(
        db=session, robot_id=1, start_time=start_time, end_time=end_time
    )
    assert len(events) == expected_len


@pytest.mark.parametrize(
    "event_id",
    invalid_ids,
)
def test_read_event_by_id_throws_404(event_id: int, session):
    with pytest.raises(HTTPException) as e:
        event: EventDBModel = get_by_id(EventDBModel, db=session, item_id=event_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value


def test_create_report(session):
    robot_id: int = 1
    isar_mission_id: str = "isar_id_test"
    echo_mission_id: int = 12345
    report_status: ReportStatus = ReportStatus.in_progress
    pre_count: int = len(get_list_paginated(ReportDBModel, session))
    report_id: int = create_report(
        db=session,
        robot_id=robot_id,
        isar_mission_id=isar_mission_id,
        echo_mission_id=echo_mission_id,
        report_status=report_status,
    )
    post_count: int = len(get_list_paginated(ReportDBModel, session))
    assert pre_count + 1 == post_count
    assert get_by_id(ReportDBModel, db=session, item_id=report_id)


def test_create_event(session):
    robot_id: int = 1
    echo_mission_id: int = 12345
    start_time = datetime.now()
    pre_count: int = len(get_list_paginated(EventDBModel, session))
    event_id: int = create_event(
        db=session,
        robot_id=robot_id,
        echo_mission_id=echo_mission_id,
        start_time=start_time,
        estimated_duration=timedelta(hours=1),
    )
    post_count: int = len(get_list_paginated(EventDBModel, session))
    assert pre_count + 1 == post_count
    assert get_by_id(EventDBModel, db=session, item_id=event_id)


def test_remove_event(session):
    event_id: int = 1
    assert get_by_id(EventDBModel, db=session, item_id=event_id)
    remove_event(db=session, event_id=event_id)
    with pytest.raises(HTTPException) as e:
        get_by_id(EventDBModel, db=session, item_id=event_id)
        assert e.value.status_code == HTTPStatus.NOT_FOUND.value
