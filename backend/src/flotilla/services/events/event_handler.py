import datetime
import time
from logging import getLogger
from typing import List

import pytz
from fastapi.exceptions import HTTPException
from pytest import Session
from requests import Response
from sqlalchemy import DateTime

from flotilla.database.crud import (
    create_report,
    read_event_by_status,
    read_robot_by_id,
    update_event_status,
)
from flotilla.database.db import SessionLocal
from flotilla.database.models import (
    EventDBModel,
    EventStatus,
    ReportStatus,
    RobotDBModel,
    RobotStatus,
)
from flotilla.services.isar.service import IsarService


def run_event_handler() -> None:
    event_handler = EventHandler()
    event_handler.main()


class EventHandler:
    def __init__(self):
        self.logger = getLogger("event handler")
        self.isar_service: IsarService = IsarService()

    def main(self) -> None:
        self.logger.info(f"Event handler started")
        while True:
            pending_events: List[EventDBModel] = self._get_pending_events()
            events = [
                e
                for e in pending_events
                if self._event_ready_to_start(start_time=e.start_time)
            ]
            for event in events:
                try:
                    db_robot = self._get_robot_for_event(event=event)

                    response_isar = self._start_isar_mission(
                        robot=db_robot, event=event
                    )

                    self.logger.info(f"Started mission:{event.echo_mission_id}")
                    self._set_event_status(
                        event_id=event.id, new_status=EventStatus.started
                    )

                    self._initialize_mission_report(
                        event=event, response_isar_json=response_isar.json()
                    )
                except RobotNotAvailableException:
                    continue
                except (
                    RobotNotFoundException,
                    IsarStartMissionException,
                ):
                    self._set_event_status(
                        event_id=event.id, new_status=EventStatus.failed
                    )
                    continue

            time.sleep(1)

    def _get_pending_events(self) -> List[EventDBModel]:
        db_session: Session = SessionLocal()
        db_events_pending: List[EventDBModel] = read_event_by_status(
            db=db_session, event_status=EventStatus.pending
        )

        SessionLocal.remove()

        return db_events_pending

    def _event_ready_to_start(self, start_time: DateTime) -> bool:
        current_time_utc = datetime.datetime.now(tz=datetime.timezone.utc)

        # Workaround to force timezone to be UTC if unspecified
        if not start_time.tzinfo:
            utc = pytz.UTC
            start_time = utc.localize(start_time)

        if current_time_utc < start_time:
            return False

        return True

    def _get_robot_for_event(self, event: EventDBModel) -> RobotDBModel:
        db_session: Session = SessionLocal()
        try:
            db_robot: RobotDBModel = read_robot_by_id(db=db_session, id=event.robot_id)

        except HTTPException as e:
            self.logger.error(
                f"Failed get robot for mission {event.echo_mission_id}, assigned robot id {event.robot_id}: {e.detail}"
            )
            SessionLocal.remove()
            raise RobotNotFoundException

        SessionLocal.remove()

        if db_robot.status != RobotStatus.available:
            error_message = f"Robot {db_robot.id} is not available for scheduled event"
            self.logger.error(error_message)
            raise RobotNotAvailableException(error_message)

        return db_robot

    def _start_isar_mission(self, robot: RobotDBModel, event: EventDBModel) -> Response:
        try:
            response_isar: Response = self.isar_service.start_mission(
                host=robot.host,
                port=robot.port,
                mission_id=event.echo_mission_id,
            )

        except HTTPException as e:
            self.logger.error(
                f"Could not start mission with id {event.echo_mission_id} for robot with id {event.robot_id}: {e.detail}"
            )
            raise IsarStartMissionException
        return response_isar

    def _set_event_status(self, event_id, new_status: EventStatus) -> None:
        db_session: Session = SessionLocal()
        try:
            update_event_status(
                db=db_session,
                event_id=event_id,
                new_status=new_status,
            )

        except HTTPException as e:
            self.logger.error(f"Failed to set event status for event {event_id}")

        SessionLocal.remove()

    def _initialize_mission_report(
        self, event: EventDBModel, response_isar_json
    ) -> int:
        db_session: Session = SessionLocal()
        report_id: int = create_report(
            db_session,
            robot_id=event.robot_id,
            isar_mission_id=response_isar_json["mission_id"],
            echo_mission_id=event.echo_mission_id,
            report_status=ReportStatus.in_progress,
        )
        SessionLocal.remove()

        return report_id


class RobotNotAvailableException(Exception):
    pass


class RobotNotFoundException(Exception):
    pass


class IsarStartMissionException(Exception):
    pass
