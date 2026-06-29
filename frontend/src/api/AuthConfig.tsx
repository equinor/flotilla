import { Configuration } from '@azure/msal-browser'
import { config } from 'config'

export const msalConfig: Configuration = {
    auth: {
        clientId: config.AD_CLIENT_ID,
        authority: 'https://login.microsoftonline.com/' + config.AD_TENANT_ID,
        redirectUri: config.FRONTEND_URL,
    },
    cache: {
        cacheLocation: 'sessionStorage',
    },
}

export const backendLoginRequest = {
    scopes: [config.BACKEND_API_SCOPE],
}

export const saraLoginRequest = {
    scopes: [config.SARA_API_SCOPE],
}
