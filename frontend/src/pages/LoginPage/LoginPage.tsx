import { useMsal } from '@azure/msal-react'
import { CircularProgress, Typography } from '@equinor/eds-core-react'
import { InteractionStatus } from '@azure/msal-browser'
import styled from 'styled-components'

const Centered = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 10px;
`

export const LoginPage = () => {
    const { inProgress } = useMsal()

    return (
        <Centered>
            <Typography variant="body_long_bold" color="primary">
                {inProgress !== InteractionStatus.None ? 'Authentication in progress...' : 'Redirecting to login...'}
            </Typography>
            <CircularProgress size={48} />
        </Centered>
    )
}
