from fastapi.security.base import SecurityBase
from fastapi_azure_auth import SingleTenantAzureAuthorizationCodeBearer

from flotilla.settings import settings


class NoSecurity(SecurityBase):
    def __init__(self) -> None:
        self.scheme_name = "No Security"


class Authenticator:
    def __init__(
        self,
        authentication_enabled: bool = settings.AUTHENTICATION_ENABLED,
        app_client_id: str = settings.APP_CLIENT_ID,
        tenant_id: str = settings.TENANT_ID,
    ) -> None:
        self.authentication_enabled: bool = authentication_enabled
        self.app_client_id: str = app_client_id
        self.tenant_id: str = tenant_id

        self.scheme: SecurityBase
        if self.authentication_enabled:
            self.scheme = SingleTenantAzureAuthorizationCodeBearer(
                app_client_id=self.app_client_id,
                tenant_id=self.tenant_id,
                scopes={
                    f"api://{self.app_client_id}/user_impersonation": "user_impersonation",
                },
            )
        else:
            self.scheme = NoSecurity

    def get_scheme(self):
        return self.scheme

    async def load_config(self):
        if self.authentication_enabled:
            await self.scheme.openid_config.load_config()


authenticator = Authenticator()
authentication_scheme = authenticator.get_scheme()
