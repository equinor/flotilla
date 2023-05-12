import { Typography } from '@equinor/eds-core-react'
import { StyledCenteredPage } from '../UnauthorizedPage/UnauthorizedPage'
import { TranslateText } from 'components/Contexts/LanguageContext'

export const UnknownErrorPage = () => {
    var errorMessage = 'An unknown error has occurred'
    return (
        <StyledCenteredPage>
            <Typography variant="h1">{TranslateText(errorMessage)}</Typography>
        </StyledCenteredPage>
    )
}
