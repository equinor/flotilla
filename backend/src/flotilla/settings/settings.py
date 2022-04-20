import importlib.resources as pkg_resources
from typing import Union

from pydantic import AnyHttpUrl, BaseSettings, Field


class Settings(BaseSettings):
    AUTHENTICATION_ENABLED: bool = Field(default=True)
    BACKEND_CORS_ORIGINS: list[Union[str, AnyHttpUrl]] = [
        "http://localhost:8000",
        "http://localhost:3001",
    ]
    SQLALCHEMY_DATABASE_URL: str = Field(default="sqlite:///:memory:")
    OPENAPI_CLIENT_ID: str = Field(default="")
    APP_CLIENT_ID: str = Field(default="")
    TENANT_ID: str = Field(default="")

    ECHO_CLIENT_ID: str = Field(default="")
    ECHO_APP_SCOPE: str = Field(default="")
    ECHO_API_URL: str = Field(default="")

    INSTALLATION_CODE: str = Field(default="")
    REQUEST_TIMEOUT: float = Field(default=5)
    ISAR_CLIENT_ID: str = Field(default="")
    ISAR_APP_SCOPE: str = Field(default="")

    PAGE: int = Field(default=0)
    MINIMUM_PAGE_NUMBER: int = Field(default=0)
    PAGE_SIZE: int = Field(default=100)
    MINIMUM_PAGE_SIZE: int = Field(default=0)
    MAX_PAGE_SIZE: int = Field(default=100)

    class Config:
        with pkg_resources.path("flotilla.settings", "settings.env") as path:
            package_path = path
        env_prefix = "FLOTILLA_"
        env_file = package_path
        env_file_encoding = "utf-8"
        case_sensitive = True


settings = Settings()
