import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'

const StyledUnknownErrorPage = styled.div`
    display: flex;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    padding-block: 20rem;
    gap: 3rem;
`

export const UnknownErrorPage = () => {
    var errorMessage = 'An unknown error has occurred'
    return (
        <StyledUnknownErrorPage>
            <Typography variant="h1">{errorMessage}</Typography>
        </StyledUnknownErrorPage>
    )
}
