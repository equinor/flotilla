import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import { StyledPage } from 'components/Styles/StyledComponents'
import styled from 'styled-components'

const StyledText = styled.div`
    display: flex;
    flex-direction: column;
    gap: 2rem;
    max-width: 1000px;
`
const StyledParagraph = styled.div`
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
`

export const InfoPage = () => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <Header page={'info'} />
            <StyledPage>
                <StyledText>
                    <StyledParagraph>
                        <Typography variant="h1">{TranslateText('Flotilla Info Page')}</Typography>
                        <Typography variant="body_short">{TranslateText('Info: Flotilla is..')}</Typography>
                    </StyledParagraph>
                    <StyledParagraph>
                        <Typography variant="h3">{TranslateText('Automatic scheduling of missions')}</Typography>
                        <Typography variant="body_short">{TranslateText('Info: Autoscheduling..')}</Typography>
                    </StyledParagraph>
                    <StyledParagraph>
                        <Typography variant="h5">{TranslateText('Updating mission tasks')}</Typography>
                        <Typography variant="body_short">{TranslateText('Info: Updating mission tasks..')}</Typography>
                    </StyledParagraph>
                </StyledText>
            </StyledPage>
        </>
    )
}
