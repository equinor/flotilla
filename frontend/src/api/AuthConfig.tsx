import { Configuration } from '@azure/msal-browser'
import { IMsalContext } from '@azure/msal-react'
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

export const loginRequest = {
    scopes: [config.BACKEND_API_SCOPE],
}

export async function fetchAccessToken(context: IMsalContext): Promise<string> {
    // Silently acquires an access token which is then attached to a request for Microsoft Graph data
    const account = context.accounts[0]
    return context.instance
        .acquireTokenSilent({ ...loginRequest, account })
        .then((response) => {
            const accessToken: string = response.accessToken ?? ''
            return accessToken
        })
        .catch((e) => {
            console.error(e)
            return context.instance.acquireTokenRedirect(loginRequest).then(() => {
                return 'The page should be refreshed automatically.'
            })
        })
}
