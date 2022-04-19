import enum
from datetime import datetime, timezone

from flotilla_openapi.models.event import Event
from flotilla_openapi.models.report import Report
from flotilla_openapi.models.report_entry import ReportEntry
from flotilla_openapi.models.robot import Robot
from sqlalchemy import (
    Boolean,
    Column,
    DateTime,
    Enum,
    ForeignKey,
    Integer,
    Interval,
    String,
)
from sqlalchemy.orm import backref, relationship

from flotilla.database.db import Base


class RobotStatus(enum.Enum):
    available = "Available"
    busy = "Busy"
    offline = "Offline"


class InspectionType(enum.Enum):
    image = "Image"
    thermal_image = "ThermalImage"

    @classmethod
    def has_value(cls, value: str) -> bool:
        return value in cls._value2member_map_


class ReportStatus(enum.Enum):
    in_progress = "InProgress"
    failed = "Failed"
    completed = "Completed"


class ReportEntryStatus(enum.Enum):
    failed = "Failed"
    completed = "Completed"


class Resource(enum.Enum):
    pose = "pose"
    battery = "battery"
    pressure = "pressure"


class RobotDBModel(Base):
    __tablename__ = "robot"
    id = Column(Integer, primary_key=True)
    name = Column(String)
    model = Column(String)
    serial_number = Column(String)
    logs = Column(String)
    status = Column(Enum(RobotStatus))
    host = Column(String)
    port = Column(Integer)
    enabled = Column(Boolean, default=True)
    telemetry_topics = relationship("TopicDBModel", backref=backref("robot"))
    streams = relationship("VideoStreamDBModel", backref=backref("robot"))
    capabilities = relationship("CapabilityDBModel", backref=backref("robot"))
    events = relationship("EventDBModel", backref=backref("robot"))

    def get_api_robot(self) -> Robot:
        return Robot(
            id=self.id,
            name=self.name,
            model=self.model,
            status=self.status.value,
            capabilities=[cap.capability.value for cap in self.capabilities],
            serial_number=self.serial_number,
            host=self.host,
            port=self.port,
            enabled=self.enabled,
        )


class ReportDBModel(Base):
    __tablename__ = "report"
    id = Column(Integer, primary_key=True)
    robot_id = Column(Integer, ForeignKey("robot.id"))
    isar_mission_id = Column(String)
    echo_mission_id = Column(Integer)
    log = Column(String)
    status = Column(Enum(ReportStatus))
    start_time = Column(DateTime(timezone=True), default=datetime.now(tz=timezone.utc))
    entries = relationship("ReportEntryDBModel", backref=backref("report"))

    def get_api_report(self) -> Report:
        return Report(
            id=self.id,
            start_time=self.start_time,
            end_time=datetime.now(tz=timezone.utc),
            robot_id=self.robot_id,
            mission_id=self.echo_mission_id,
            status=self.status.value,
            entries=[entry.get_report_entry() for entry in self.entries],
        )


class MapDBModel(Base):
    __tablename__ = "map"
    id = Column(Integer, primary_key=True)
    location = Column(String)
    coordinate_reference_system = Column(String)


class TopicDBModel(Base):
    __tablename__ = "topic"
    id = Column(Integer, primary_key=True)
    robot_id = Column(Integer, ForeignKey("robot.id"))
    path = Column(String)
    resource = Column(Enum(Resource))


class VideoStreamDBModel(Base):
    __tablename__ = "video_stream"
    id = Column(Integer, primary_key=True)
    robot_id = Column(Integer, ForeignKey("robot.id"))
    rtsp_url = Column(String)
    port = Column(Integer)
    camera_name = Column(String)


class CapabilityDBModel(Base):
    __tablename__ = "capability"
    id = Column(Integer, primary_key=True)
    robot_id = Column(Integer, ForeignKey("robot.id"))
    capability = Column(Enum(InspectionType))


def event_end_time_default(context):
    start_time = context.get_current_parameters()["start_time"]
    duration = context.get_current_parameters()["estimated_duration"]
    return start_time + duration


class EventDBModel(Base):
    __tablename__ = "event"
    id = Column(Integer, primary_key=True)
    robot_id = Column(Integer, ForeignKey("robot.id"))
    echo_mission_id = Column(Integer)
    report_id = Column(Integer, ForeignKey("report.id"))
    start_time = Column(DateTime(timezone=True), default=datetime.now(tz=timezone.utc))
    estimated_duration = Column(Interval)
    end_time = Column(DateTime(timezone=True), default=event_end_time_default)
    # TODO: robot_id and report_id.robot_id can now point at different robots.
    # Should there be a constraint forcing an event to point at only one robot?

    def get_api_event(self) -> Event:
        return Event(
            id=self.id,
            robot_id=self.robot_id,
            mission_id=self.echo_mission_id,
            start_time=self.start_time,
            end_time=self.end_time,
        )


class ReportEntryDBModel(Base):
    __tablename__ = "entry"
    id = Column(Integer, primary_key=True)
    report_id = Column(Integer, ForeignKey("report.id"))
    tag_id = Column(String)
    status = Column(Enum(ReportEntryStatus))
    inspection_type = Column(Enum(InspectionType))
    time = Column(DateTime(timezone=True), default=datetime.now(tz=timezone.utc))
    file_location = Column(String)

    def get_report_entry(self) -> ReportEntry:
        return ReportEntry(
            id=self.id,
            tag_id=self.tag_id,
            status=self.status.value,
            inspection_type=self.inspection_type.value,
            time=self.time,
            link=None,
        )
