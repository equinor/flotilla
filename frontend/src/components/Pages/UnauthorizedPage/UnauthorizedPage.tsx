import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { GoToAccessITButton } from './GoToAccessITButton'

const StyledUnauthorizedPage = styled.div`
    display: flex;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    padding-block: 20rem;
    gap: 3rem;
`

export const UnauthorizedPage = () => {
    var errorMessage = "You don't have access to this site. Apply for access in AccessIT"
    return (
        <StyledUnauthorizedPage>
            <Typography variant="h1">{errorMessage}</Typography>
            <GoToAccessITButton />
        </StyledUnauthorizedPage>
    )
}
