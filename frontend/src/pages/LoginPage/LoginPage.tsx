import { useMsal } from '@azure/msal-react'
import { CircularProgress, Typography } from '@equinor/eds-core-react'
import { InteractionStatus } from '@azure/msal-browser'
import styled from 'styled-components'
import { useEffect, useRef } from 'react'
import { loginRequest } from 'api/AuthConfig'

const Centered = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 10px;
`

export const LoginPage = () => {
    const { instance, accounts, inProgress } = useMsal()
    const didRedirect = useRef(false)

    useEffect(() => {
        if (accounts.length > 0 || instance.getActiveAccount()) return

        if (inProgress !== InteractionStatus.None) return

        if (didRedirect.current) return
        didRedirect.current = true

        instance.loginRedirect(loginRequest)
    }, [accounts.length, inProgress, instance])

    return (
        <Centered>
            <Typography variant="body_long_bold" color="primary">
                {inProgress !== InteractionStatus.None ? 'Authentication in progress...' : 'Redirecting to login...'}
            </Typography>
            <CircularProgress size={48} />
        </Centered>
    )
}
