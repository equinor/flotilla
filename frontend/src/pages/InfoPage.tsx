import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import { StyledPage } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import infoPageImage from 'mediaAssets/taurob-about-us.png'
import { phone_width } from 'utils/constants'

const StyledInfoPAge = styled(StyledPage)`
    width: 100%;
    margin-left: 0px;
    padding-left: 0px;
`

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
    width: 600px;
    @media (max-width: ${phone_width}) {
        width: 350px;
    }
`

const StyledImage = styled.img`
    max-heigth: 100%;
    max-width: 45%;
    padding-left: 20px;

    @media (max-width: ${phone_width}) {
        max-width: 100vw;
        padding: 0px;
    }
`

const StyledInfoSection = styled.div`
    display: flex;
    flex-direction: row;
    background-color: ${tokens.colors.ui.background__default.rgba};
    width: 100vw;
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
        padding-left: 15px;
    }
`

const JustifiedTypography = styled(Typography)`
    text-align: justify;
`

export const InfoPage = () => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <Header page={'info'} />
            <StyledInfoPAge>
                <StyledAboutSection>
                    <Typography variant="h1">{TranslateText('About Flotilla')}</Typography>
                    <StyledTypography variant="body_short">{TranslateText('Info: Flotilla is..')}</StyledTypography>
                </StyledAboutSection>
                <StyledInfoSection>
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
                </StyledInfoSection>
            </StyledInfoPAge>
        </>
    )
}
