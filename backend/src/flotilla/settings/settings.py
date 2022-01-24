import importlib.resources as pkg_resources
from typing import Union

from pydantic import AnyHttpUrl, BaseSettings, Field


class Settings(BaseSettings):
    AUTHENTICATION_ENABLED: bool = Field(default=True, env="AUTHENTICATION_ENABLED")
    BACKEND_CORS_ORIGINS: list[Union[str, AnyHttpUrl]] = ["http://localhost:8000"]
    OPENAPI_CLIENT_ID: str = Field(default="", env="OPENAPI_CLIENT_ID")
    APP_CLIENT_ID: str = Field(default="", env="APP_CLIENT_ID")
    TENANT_ID: str = Field(default="", env="TENANT_ID")

    class Config:
        with pkg_resources.path("flotilla.settings", "settings.env") as path:
            package_path = path
        env_file = package_path
        env_file_encoding = "utf-8"
        case_sensitive = True


settings = Settings()
