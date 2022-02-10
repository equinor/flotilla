from typing import List, Optional

from sqlalchemy.orm import Session

from flotilla.database.models import ReportDBModel, ReportStatus, RobotDBModel


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
