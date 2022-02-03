from typing import List

from sqlalchemy.orm import Session

from flotilla.database.models import Report, ReportStatus, Robot


def read_robots(db: Session) -> List[Robot]:
    return db.query(Robot).all()


def read_robot_by_id(db: Session, robot_id: int) -> Robot:
    return db.query(Robot).filter(Robot.id == robot_id).first()


def create_report(
    db: Session,
    robot_id: int,
    isar_mission_id: str,
    echo_mission_id: int,
    report_status: ReportStatus,
) -> int:
    report: Report = Report(
        robot_id=robot_id,
        isar_mission_id=isar_mission_id,
        echo_mission_id=echo_mission_id,
        log="",
        status=report_status,
    )
    db.add(report)
    db.commit()
    return report.id
