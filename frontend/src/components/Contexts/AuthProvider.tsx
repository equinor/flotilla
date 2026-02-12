import { useMsal } from '@azure/msal-react'
import { useCallback, useMemo } from 'react'
import { loginRequest } from 'api/AuthConfig'
import { AuthContext } from './AuthContext'
import { InteractionRequiredAuthError } from '@azure/msal-browser'

type Props = {
    children?: React.ReactNode
}

export const AuthProvider = ({ children }: Props) => {
    const { instance, accounts, inProgress } = useMsal()

    const isAuthenticated = !!(instance.getActiveAccount() ?? accounts[0])

    const getAccessToken = useCallback(async () => {
        const account = instance.getActiveAccount() ?? accounts[0]
        if (!account) throw new Error('No signed-in account found.')

        try {
            const resp = await instance.acquireTokenSilent({ ...loginRequest, account })
            return resp.accessToken
        } catch (e: any) {
            // 1) If MSAL is already doing an interactive flow, do NOT start another
            if (e?.errorCode === 'interaction_in_progress' || inProgress !== 'none') {
                throw e
            }

            // 2) If an interactive login/consent is required, trigger redirect once
            const interactionRequired =
                e instanceof InteractionRequiredAuthError ||
                e?.errorCode === 'interaction_required' ||
                e?.errorCode === 'consent_required' ||
                e?.errorCode === 'login_required'

            if (interactionRequired) {
                await instance.acquireTokenRedirect({ ...loginRequest, account })
                // Redirect navigates away; this is mostly to satisfy TS control flow
                throw e
            }

            // 3) Otherwise bubble up
            throw e
        }
    }, [accounts, inProgress, instance])

    const value = useMemo(() => ({ getAccessToken, isAuthenticated }), [getAccessToken, isAuthenticated])

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
