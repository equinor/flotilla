import { Typography } from '@equinor/eds-core-react'
import { StyledCenteredPage } from 'components/Pages/UnauthorizedPage/UnauthorizedPage'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

export const UnknownErrorPage = () => {
    const { TranslateText } = useLanguageContext()
    const errorMessage = 'An unknown error has occurred'
    return (
        <StyledCenteredPage>
            <Typography variant="h1">{TranslateText(errorMessage)}</Typography>
        </StyledCenteredPage>
    )
}
