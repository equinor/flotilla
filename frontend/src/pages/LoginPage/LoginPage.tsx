import { useEffect, useRef } from 'react'
import { useIsAuthenticated } from '@azure/msal-react'
import { useMsal } from '@azure/msal-react'
import { loginRequest } from 'api/AuthConfig'
import { CircularProgress, Typography } from '@equinor/eds-core-react'
import { InteractionStatus, IPublicClientApplication } from '@azure/msal-browser'
import styled from 'styled-components'

const Centered = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 10px;
`

const handleLogin = (instance: IPublicClientApplication) => {
    const accounts = instance.getAllAccounts()
    if (accounts.length > 0) {
        console.log('Active account found, skipping login')
        return
    }

    instance.loginRedirect(loginRequest).catch((e) => {
        console.error('Login error:', e)
    })
}

export const LoginPage = () => {
    const isAuthenticated = useIsAuthenticated()
    const { instance, inProgress } = useMsal()
    const loginInitiatedRef = useRef(false)

    useEffect(() => {
        // Trigger login when not authenticated and no interaction is in progress
        // Use ref to prevent double-triggering in StrictMode
        if (!isAuthenticated && inProgress === InteractionStatus.None && !loginInitiatedRef.current) {
            console.log('Not authenticated, initiating login...')
            loginInitiatedRef.current = true
            handleLogin(instance)
        }
    }, [isAuthenticated, inProgress, instance])

    // Show loading while authentication is in progress
    if (inProgress !== InteractionStatus.None) {
        return (
            <Centered>
                <Typography variant="body_long_bold" color="primary">
                    Authentication in progress...
                </Typography>
                <CircularProgress size={48} />
            </Centered>
        )
    }

    return (
        <Centered>
            <Typography variant="body_long_bold" color="primary">
                Redirecting to login...
            </Typography>
            <CircularProgress size={48} />
        </Centered>
    )
}
