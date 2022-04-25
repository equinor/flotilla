from abc import ABC, abstractmethod
from datetime import datetime, timedelta
from typing import List, TypeVar

from fastapi import Query
from pydantic import BaseModel
from sqlalchemy import and_
from sqlalchemy.orm import Query as DBQuery
from sqlalchemy.orm import Session

from flotilla.database.db import Base
from flotilla.database.models import EventDBModel, ReportDBModel, ReportStatus

T = TypeVar("T", bound=Base)  # Generic type for db models


class Filter:
    def __init__(self, model_type: type[T]) -> None:
        self.model_type: type[T] = model_type
        self.filters: List[SubFilter] = []

    def filter(self, db: Session):
        query: DBQuery = db.query(self.model_type)
        for _filter in self.filters:
            query = _filter.filter(query=query, model_type=self.model_type)
        return query


class EventFilterParams(BaseModel):
    min_start_time: datetime = Query(None)
    max_start_time: datetime = Query(None)
    robot_id: int = Query(None)

    def __init__(__pydantic_self__, **data) -> None:
        super().__init__(**data)
        if not __pydantic_self__.min_start_time:
            __pydantic_self__.min_start_time = datetime.utcnow()
        if not __pydantic_self__.max_start_time:
            __pydantic_self__.max_start_time = (
                __pydantic_self__.min_start_time + timedelta(days=7)
            )


class EventFilter(Filter):
    def __init__(self, params: EventFilterParams) -> None:
        super().__init__(model_type=EventDBModel)
        self.filters.append(
            StartTimeFilter(
                min_time=params.min_start_time,
                max_time=params.max_start_time,
            )
        )
        if params.robot_id:
            self.filters.append(RobotIdFilter(robot_id=params.robot_id))


class ReportFilterParams(BaseModel):
    min_start_time: datetime = Query(None)
    max_start_time: datetime = Query(None)
    robot_id: int = Query(None)
    status: ReportStatus = Query(None)

    def __init__(__pydantic_self__, **data) -> None:
        super().__init__(**data)
        if not __pydantic_self__.max_start_time:
            __pydantic_self__.max_start_time = datetime.utcnow()
        if not __pydantic_self__.min_start_time:
            __pydantic_self__.min_start_time = (
                __pydantic_self__.max_start_time - timedelta(days=7)
            )


class ReportFilter(Filter):
    def __init__(self, params: ReportFilterParams) -> None:
        super().__init__(model_type=ReportDBModel)
        if params.robot_id:
            self.filters.append(RobotIdFilter(robot_id=params.robot_id))
        if params.status:
            self.filters.append(StatusFilter(status=str(params.status)))
        self.filters.append(
            StartTimeFilter(
                min_time=params.min_start_time,
                max_time=params.max_start_time,
            )
        )


class SubFilter(ABC):
    @abstractmethod
    def filter(self, query: DBQuery, model_type: type[T]):
        pass


class RobotIdFilter(SubFilter):
    def __init__(self, robot_id) -> None:
        self.robot_id = robot_id

    def filter(self, query: DBQuery, model_type: type[T]):
        return query.filter(model_type.robot_id == self.robot_id)


class StartTimeFilter(SubFilter):
    def __init__(self, min_time, max_time) -> None:
        self.min_time = min_time
        self.max_time = max_time

    def filter(self, query: DBQuery, model_type: type[T]):
        return query.filter(
            and_(
                model_type.start_time >= self.min_time,
                model_type.start_time < self.max_time,
            )
        )


class StatusFilter(SubFilter):
    def __init__(self, status: str) -> None:
        self.status: str = status

    def filter(self, query, model_type):
        return query.filter(model_type.status == self.status)
