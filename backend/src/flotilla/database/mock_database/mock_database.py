from datetime import datetime, timedelta, timezone

from flotilla.database.models import (
    CapabilityDBModel,
    EventDBModel,
    EventStatus,
    InspectionType,
    ReportDBModel,
    ReportEntryDBModel,
    ReportEntryStatus,
    ReportStatus,
    Resource,
    RobotDBModel,
    RobotStatus,
    TopicDBModel,
)


def populate_mock_db(session, connection, base) -> None:

    # If database already configured/populated - abort
    if base.metadata.bind == connection:
        return

    base.metadata.bind = connection
    base.metadata.create_all()

    robot_1 = RobotDBModel(
        name="Harald",
        model="King",
        serial_number="V",
        logs="",
        status=RobotStatus.available,
        host="localhost",
        port=3000,
    )

    robot_2 = RobotDBModel(
        name="Haakon",
        model="King",
        serial_number="VII",
        logs="",
        status=RobotStatus.offline,
        host="localhost",
        port=3002,
    )

    session.add_all([robot_1, robot_2])
    session.commit()

    report_1 = ReportDBModel(
        robot_id=robot_1.id,
        isar_mission_id="isar_mission_id",
        echo_mission_id=1,
        log="",
        status=ReportStatus.in_progress,
        start_time=datetime.utcnow() - timedelta(hours=0.5),
    )

    report_2 = ReportDBModel(
        robot_id=robot_2.id,
        isar_mission_id="isar_mission_id",
        echo_mission_id=1,
        log="",
        status=ReportStatus.completed,
        start_time=datetime.utcnow() - timedelta(hours=2),
        end_time=datetime.utcnow() - timedelta(hours=1),
    )

    session.add_all([report_1, report_2])
    session.commit()

    entry_1 = ReportEntryDBModel(
        report_id=report_1.id,
        tag_id="tag_id",
        status=ReportEntryStatus.completed,
        inspection_type=InspectionType.image,
        time=datetime.now(tz=timezone.utc),
        file_location="",
    )

    entry_2 = ReportEntryDBModel(
        report_id=report_2.id,
        tag_id="tag_id",
        status=ReportEntryStatus.failed,
        inspection_type=InspectionType.thermal_image,
        time=datetime.now(tz=timezone.utc),
        file_location="",
    )

    entry_3 = ReportEntryDBModel(
        report_id=report_2.id,
        tag_id="tag_id",
        status=ReportEntryStatus.completed,
        inspection_type=InspectionType.image,
        time=datetime.now(tz=timezone.utc),
        file_location="",
    )

    event_1 = EventDBModel(
        robot_id=robot_1.id,
        echo_mission_id=1,
        report_id=report_1.id,
        start_time=datetime.now(tz=timezone.utc) - timedelta(hours=2),
        estimated_duration=timedelta(hours=1),
        status=EventStatus.completed,
    )

    event_2 = EventDBModel(
        robot_id=robot_2.id,
        echo_mission_id=287,
        report_id=report_2.id,
        estimated_duration=timedelta(hours=2),
        status=EventStatus.failed,
    )

    capability_1 = CapabilityDBModel(
        robot_id=robot_2.id,
        capability=InspectionType.image,
    )

    capability_2 = CapabilityDBModel(
        robot_id=robot_2.id,
        capability=InspectionType.thermal_image,
    )

    capability_3 = CapabilityDBModel(
        robot_id=robot_1.id,
        capability=InspectionType.image,
    )

    topic_1 = TopicDBModel(
        robot_id=robot_1.id, path="/robot_1/pose", resource=Resource.pose
    )

    topic_2 = TopicDBModel(
        robot_id=robot_1.id, path="/robot_1/battery", resource=Resource.battery
    )

    topic_3 = TopicDBModel(
        robot_id=robot_2.id, path="/robot_2/pressure", resource=Resource.pressure
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
            topic_3,
        ]
    )
    session.commit()
