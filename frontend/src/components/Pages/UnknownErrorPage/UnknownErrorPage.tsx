import { Typography } from '@equinor/eds-core-react'
import { StyledCenteredPage } from '../UnauthorizedPage/UnauthorizedPage'
import { Text } from 'components/Contexts/LanguageContext'

export const UnknownErrorPage = () => {
    var errorMessage = 'An unknown error has occurred'
    return (
        <StyledCenteredPage>
            <Typography variant="h1">{Text(errorMessage)}</Typography>
        </StyledCenteredPage>
    )
}
