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
    scopes: ['api://ea4c7b92-47b3-45fb-bd25-a8070f0c495c/user_impersonation'],
}

export async function fetchAccessToken(context: IMsalContext): Promise<string> {
    // Silently acquires an access token which is then attached to a request for Microsoft Graph data
    const account = context.accounts[0]
    return context.instance
        .acquireTokenSilent({ ...loginRequest, account })
        .then((response) => {
            const accessToken: string = response.accessToken ?? ''
            console.log('Fetched cached token')
            return accessToken
        })
        .catch((e) => {
            console.log(e)
            return context.instance.acquireTokenRedirect(loginRequest).then((response) => {
                console.log('THIS SHOULD NOT HAPPEN LOLOLOOLOLOL')
                return 'The page should be refreshed automatically.'
            })
        })
}
