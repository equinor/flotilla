import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Button } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

export const StyledCenteredPage = styled.div`
    display: flex;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    padding-block: 20rem;
    gap: 3rem;
`

const GoToAccessITButton = () => {
    const { TranslateText } = useLanguageContext()
    return (
        <Button href="https://accessit.equinor.com" variant="contained">
            {TranslateText('Go to AccessIT')}
        </Button>
    )
}

export const UnauthorizedPage = () => {
    const { TranslateText } = useLanguageContext()
    const errorMessage = "You don't have access to this site. Apply for access in AccessIT"
    return (
        <StyledCenteredPage>
            <Typography variant="h1">{TranslateText(errorMessage)}</Typography>
            <GoToAccessITButton />
        </StyledCenteredPage>
    )
}
