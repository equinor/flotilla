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
    const account = context.accounts[0]

    return context.instance
        .acquireTokenSilent({ ...loginRequest, account })
        .then((response) => {
            const accessToken: string = response.accessToken ?? ''
            return accessToken
        })
        .catch(async (e) => {
            if (e.errorCode === 'interaction_in_progress') {
                console.log('Token acquisition already in progress, waiting...')
                await new Promise((resolve) => setTimeout(resolve, 1000))
            }
            console.error('Token acquisition failed:', e)
            await context.instance.acquireTokenRedirect(loginRequest)
            return 'The page should be refreshed automatically.'
        })
}
