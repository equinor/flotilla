import { Typography } from '@equinor/eds-core-react'
import { StyledCenteredPage } from '../UnauthorizedPage/UnauthorizedPage'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

export const UnknownErrorPage = () => {
    const { TranslateText } = useLanguageContext()
    var errorMessage = 'An unknown error has occurred'
    return (
        <StyledCenteredPage>
            <Typography variant="h1">{TranslateText(errorMessage)}</Typography>
        </StyledCenteredPage>
    )
}
