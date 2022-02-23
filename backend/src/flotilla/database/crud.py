import datetime
from typing import List, Optional

from sqlalchemy.orm import Session

from flotilla.database.models import (
    EventDBModel,
    ReportDBModel,
    ReportStatus,
    RobotDBModel,
)


class DBException(Exception):
    pass


def read_robots(db: Session) -> List[RobotDBModel]:
    robots: List[RobotDBModel] = db.query(RobotDBModel).all()
    return robots


def read_robot_by_id(db: Session, robot_id: int) -> RobotDBModel:
    robot: Optional[RobotDBModel] = (
        db.query(RobotDBModel).filter(RobotDBModel.id == robot_id).first()
    )
    if not robot:
        raise DBException(f"No robot with id {robot_id}")
    return robot


def read_reports(db: Session) -> List[ReportDBModel]:
    reports: List[ReportDBModel] = db.query(ReportDBModel).all()
    return reports


def read_report_by_id(db: Session, report_id: int) -> ReportDBModel:
    report: Optional[ReportDBModel] = (
        db.query(ReportDBModel).filter(ReportDBModel.id == report_id).first()
    )
    if not report:
        raise DBException(f"No report with id {report_id}")
    return report


def read_events(db: Session) -> List[EventDBModel]:
    events: List[EventDBModel] = db.query(EventDBModel).all()
    return events


def read_event_by_id(db: Session, event_id: int) -> EventDBModel:
    event: Optional[EventDBModel] = (
        db.query(EventDBModel).filter(EventDBModel.id == event_id).first()
    )
    if not event:
        raise DBException(f"No event with id {event_id}")
    return event


def create_report(
    db: Session,
    robot_id: int,
    isar_mission_id: str,
    echo_mission_id: int,
    report_status: ReportStatus,
) -> int:
    report: ReportDBModel = ReportDBModel(
        robot_id=robot_id,
        isar_mission_id=isar_mission_id,
        echo_mission_id=echo_mission_id,
        log="",
        status=report_status,
    )
    db.add(report)
    db.commit()
    return report.id


def create_event(
    db: Session,
    robot_id: int,
    echo_mission_id: int,
    start_time: datetime.datetime,
) -> int:

    event: EventDBModel = EventDBModel(
        robot_id=robot_id,
        echo_mission_id=echo_mission_id,
        report_id=None,
        start_time=start_time,
        estimated_duration=datetime.timedelta(hours=1),
    )
    db.add(event)
    db.commit()
    return event.id


def remove_event(db: Session, event_id: int) -> None:
    event: EventDBModel = read_event_by_id(db, event_id)
    db.delete(event)
    db.commit()
    return
