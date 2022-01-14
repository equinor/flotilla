import datetime
from unicodedata import name

from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

from flotilla.database.models import (
    Base,
    Entry,
    EntryStatus,
    InspectionType,
    Report,
    ReportStatus,
    Robot,
    RobotStatus,
)

# engine = create_engine("sqlite:///mock.db", echo=True)
engine = create_engine("sqlite:///:memory:", echo=True)
Base.metadata.create_all(engine)
Session = sessionmaker(bind=engine)
session = Session()

robot_1 = Robot(
    name="Harald",
    model="King",
    serial_number="VI",
    logs="",
    status=RobotStatus.available,
)

robot_2 = Robot(
    name="Harold",
    model="King",
    serial_number="VII",
    logs="",
    status=RobotStatus.offline,
)

session.add_all([robot_1, robot_2])
session.commit()

report = Report(
    robot_id=robot_1.id,
    isar_mission_id="isar_mission_id",
    echo_mission_id=1,
    log="",
    status=ReportStatus.in_progress,
)

session.add(report)
session.commit()

entry = Entry(
    report_id=report.id,
    tag_id="tag_id",
    status=EntryStatus.completed,
    inspection_type=InspectionType.image,
    time=datetime.datetime.now(),
    file_location="",
)

session.add(entry)
session.commit()

print(session.query(Robot.id).all())
print(session.query(Report.robot_id).all())
for report in session.query(Report).all():
    for entry in report.entries:
        print(entry.id)
