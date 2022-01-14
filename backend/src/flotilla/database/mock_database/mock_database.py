import datetime
import resource

from flotilla.database.models import (
    Capability,
    Entry,
    EntryStatus,
    Event,
    InspectionType,
    Report,
    ReportStatus,
    Resource,
    Robot,
    RobotStatus,
    Topic,
)


def populate_mock_db(session, engine, base) -> None:
    base.metadata.create_all(engine)

    robot_1 = Robot(
        name="Harald",
        model="King",
        serial_number="V",
        logs="",
        status=RobotStatus.available,
    )

    robot_2 = Robot(
        name="Haakon",
        model="King",
        serial_number="VII",
        logs="",
        status=RobotStatus.offline,
    )

    session.add_all([robot_1, robot_2])
    session.commit()

    report_1 = Report(
        robot_id=robot_1.id,
        isar_mission_id="isar_mission_id",
        echo_mission_id=1,
        log="",
        status=ReportStatus.in_progress,
    )
    report_2 = Report(
        robot_id=robot_2.id,
        isar_mission_id="isar_mission_id",
        echo_mission_id=1,
        log="",
        status=ReportStatus.completed,
    )

    session.add(report_1, report_2)
    session.commit()

    entry_1 = Entry(
        report_id=report_1.id,
        tag_id="tag_id",
        status=EntryStatus.completed,
        inspection_type=InspectionType.image,
        time=datetime.datetime.now(tz=datetime.timezone.utc),
        file_location="",
    )

    entry_2 = Entry(
        report_id=report_2.id,
        tag_id="tag_id",
        status=EntryStatus.failed,
        inspection_type=InspectionType.thermal_image,
        time=datetime.datetime.now(tz=datetime.timezone.utc),
        file_location="",
    )

    entry_3 = Entry(
        report_id=report_2.id,
        tag_id="tag_id",
        status=EntryStatus.completed,
        inspection_type=InspectionType.image,
        time=datetime.datetime.now(tz=datetime.timezone.utc),
        file_location="",
    )

    event_1 = Event(
        robot_id=robot_1.id,
        echo_mission_id=287,
        report_id=report_1.id,
        estimated_duration=datetime.timedelta(hours=1),
    )

    event_2 = Event(
        robot_id=robot_2.id,
        echo_mission_id=287,
        report_id=report_2.id,
        estimated_duration=datetime.timedelta(hours=2),
    )

    capability_1 = Capability(
        robot_id=robot_2.id,
        capability=InspectionType.image,
    )

    capability_2 = Capability(
        robot_id=robot_2.id,
        capability=InspectionType.thermal_image,
    )

    capability_3 = Capability(
        robot_id=robot_1.id,
        capability=InspectionType.image,
    )

    topic_1 = Topic(
        robot_id=robot_1.id,
        path="/robot_1/pose",
        resource=Resource.pose
    )

    topic_2 = Topic(
        robot_id=robot_1.id,
        path="/robot_1/battery",
        resource=Resource.battery
    )

    topic_3 = Topic(
        robot_id=robot_2.id,
        path="/robot_2/pressure",
        resource=Resource.pressure 
    )
    
    session.add_all(
        [
            entry_1,
            entry_2,
            entry_3,
            event_1,
            event_2,
            capability_1,
            capability_2,
            capability_3,
            topic_1,
            topic_2,
            topic_3
        ]
    )
    session.commit()
