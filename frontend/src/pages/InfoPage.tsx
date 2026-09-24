import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import { PageContent, pageContentWidth, PageBackground } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import infoPageImage from 'mediaAssets/taurob-about-us.png'
import { phone_width } from 'utils/constants'

const StyledParagraph = styled.div`
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
`
const StyledAboutSection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    justify-content: center;
    align-items: center;
    height: 250px;
`

const StyledTypography = styled(Typography)`
    text-align: center;
    max-width: 600px;
`

const StyledImage = styled.img`
    max-height: 100%;
    max-width: 45%;
    padding-left: 20px;

    @media (max-width: ${phone_width}) {
        max-width: 100%;
        padding: 0px;
    }
`

const StyledInfoSection = styled.div`
    background-color: ${tokens.colors.ui.background__default.rgba};
`

const StyledInfoSectionContent = styled.div`
    ${pageContentWidth}
    display: flex;
    flex-direction: row;
    justify-content: center;
    align-items: top;
    gap: 20px;

    @media (max-width: ${phone_width}) {
        flex-direction: column;
        gap: 2rem;
        padding-top: 20px;
    }
`

const StyledInfo = styled.div`
    display: flex;
    flex-direction: column;
    width: 50%;
    gap: 2rem;
    padding-top: 50px;

    @media (max-width: ${phone_width}) {
        width: 90%;
    }
`

const JustifiedTypography = styled(Typography)`
    text-align: justify;
`

export const InfoPage = () => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <Header />
            <PageBackground>
                <PageContent>
                    <StyledAboutSection>
                        <Typography variant="h1">{TranslateText('About Flotilla')}</Typography>
                        <StyledTypography variant="body_short">{TranslateText('Info: Flotilla is..')}</StyledTypography>
                    </StyledAboutSection>
                </PageContent>
                <StyledInfoSection>
                    <StyledInfoSectionContent>
                        <StyledInfo>
                            <StyledParagraph>
                                <JustifiedTypography variant="h3">
                                    {TranslateText('Automatic scheduling of missions')}
                                </JustifiedTypography>
                                <JustifiedTypography variant="body_short">
                                    {TranslateText('Info: Autoscheduling..')}
                                </JustifiedTypography>
                            </StyledParagraph>
                        </StyledInfo>
                        <StyledImage src={infoPageImage} />
                    </StyledInfoSectionContent>
                </StyledInfoSection>
            </PageBackground>
        </>
    )
}
