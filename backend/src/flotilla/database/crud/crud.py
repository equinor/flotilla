from typing import List

from sqlalchemy.orm import Session

from flotilla.database.models import ReportDBModel, ReportStatus, RobotDBModel


def read_robots(db: Session) -> List[RobotDBModel]:
    return db.query(RobotDBModel).all()


def read_robot_by_id(db: Session, robot_id: int) -> RobotDBModel:
    return db.query(RobotDBModel).filter(RobotDBModel.id == robot_id).first()


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
