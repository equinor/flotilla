import datetime
import enum

from flotilla_openapi.models.robot import Robot
from sqlalchemy import Column, DateTime, Enum, ForeignKey, Integer, Interval, String
from sqlalchemy.orm import backref, relationship

from flotilla.database.db import Base


class RobotStatus(enum.Enum):
    available = "Available"
    busy = "Busy"
    offline = "Offline"


class InspectionType(enum.Enum):
    image = "Image"
    thermal_image = "ThermalImage"


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
        )


class ReportDBModel(Base):
    __tablename__ = "report"
    id = Column(Integer, primary_key=True)
    robot_id = Column(Integer, ForeignKey("robot.id"))
    isar_mission_id = Column(String)
    echo_mission_id = Column(Integer)
    log = Column(String)
    status = Column(Enum(ReportStatus))
    start_time = Column(
        DateTime(timezone=True), default=datetime.datetime.now(tz=datetime.timezone.utc)
    )
    entries = relationship("ReportEntryDBModel", backref=backref("report"))


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


class EventDBModel(Base):
    __tablename__ = "event"
    id = Column(Integer, primary_key=True)
    robot_id = Column(Integer, ForeignKey("robot.id"))
    echo_mission_id = Column(Integer)
    report_id = Column(Integer, ForeignKey("report.id"))
    start_time = Column(
        DateTime(timezone=True), default=datetime.datetime.now(tz=datetime.timezone.utc)
    )
    estimated_duration = Column(Interval)
    # TODO: robot_id and report_id.robot_id can now point at different robots.
    # Should there be a constraint forcing an event to point at only one robot?


class ReportEntryDBModel(Base):
    __tablename__ = "entry"
    id = Column(Integer, primary_key=True)
    report_id = Column(Integer, ForeignKey("report.id"))
    tag_id = Column(String)
    status = Column(Enum(ReportEntryStatus))
    inspection_type = Column(Enum(InspectionType))
    time = Column(
        DateTime(timezone=True), default=datetime.datetime.now(tz=datetime.timezone.utc)
    )
    file_location = Column(String)
