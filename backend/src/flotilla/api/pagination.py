from fastapi import Query
from pydantic import BaseModel

from flotilla.settings import settings


class PaginationParams(BaseModel):
    page: int = Query(default=settings.PAGE, ge=settings.MINIMUM_PAGE_NUMBER)
    page_size: int = Query(
        default=settings.PAGE_SIZE,
        ge=settings.MINIMUM_PAGE_SIZE,
        le=settings.MAX_PAGE_SIZE,
    )
