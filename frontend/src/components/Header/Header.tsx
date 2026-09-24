import { Button, EdsProvider, Icon, TopBar } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { SelectLanguage } from 'components/Header/LanguageSelector'
import { Icons } from 'utils/icons'
import { AlertBanner } from 'components/Alerts/AlertsBanner'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { AlertIcon } from 'components/Header/AlertIcon'
import { useNavigate } from 'react-router'
import { phone_width, top_bar_height } from 'utils/constants'
import { Installation } from 'models/Installation'
import { useState } from 'react'
import { FeedbackDialog } from 'components/Dialogs/FeedbackDialog'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useAlertContext } from 'contexts/AlertContext'
import { pageContentWidth } from 'components/Styles/StyledComponents'

const TopBarBackground = styled.div`
    background: ${tokens.colors.ui.background__default.hex};
    border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
`
const StyledTopBar = styled(TopBar)`
    ${pageContentWidth}
    align-items: center;
    box-shadow: none;
    border-bottom: none;
    background: transparent;
    height: ${top_bar_height};
    @media (max-width: ${phone_width}) {
        grid-column-gap: 8px;
        height: auto;
        min-height: ${top_bar_height};
    }
`
const AppName = styled.span`
    font-size: 1rem;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__default.hex};
    cursor: pointer;
`
const InstallationName = styled.span`
    font-size: 0.68rem;
    font-weight: 500;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
    margin-left: 12px;
    padding-left: 12px;
    border-left: 1px solid ${tokens.colors.ui.background__medium.hex};
    @media (max-width: ${phone_width}) {
        margin-left: 0;
        padding-left: 0;
        border-left: none;
        font-size: 0.55rem;
    }
`
const IconStyle = styled.div`
    display: flex;
    align-items: center;
    gap: 0.8rem;
    @media (max-width: ${phone_width}) {
        gap: 0.1rem;
    }
`
const SelectLanguageWrapper = styled.div`
    margin-left: 1rem;
`
const AppWrapper = styled.div`
    display: flex;
    align-items: center;
    @media (max-width: ${phone_width}) {
        flex-direction: column;
        align-items: flex-start;
        padding: 6px 0;
    }
`

interface Props {
    installation?: Installation
}

export const Header = ({ installation }: Props) => {
    const navigate = useNavigate()
    const { TranslateText } = useLanguageContext()
    const { banner, clearBanner } = useAlertContext()
    const [isFeedbackOpen, setIsFeedbackOpen] = useState(false)

    return (
        <>
            <TopBarBackground>
                <StyledTopBar id={FrontPageSectionId.TopBar}>
                    <TopBar.Header onClick={() => navigate(`/${installation?.installationCode || ''}`)}>
                        <AppWrapper>
                            <AppName>Flotilla</AppName>
                            {installation && <InstallationName>{installation.name}</InstallationName>}
                        </AppWrapper>
                    </TopBar.Header>
                    <TopBar.Actions>
                        <EdsProvider density="compact">
                            <IconStyle>
                                <Button
                                    variant="ghost_icon"
                                    aria-label={TranslateText('Send feedback')}
                                    onClick={() => setIsFeedbackOpen(true)}
                                >
                                    <Icon name={Icons.Feedback} size={24} title={TranslateText('Send feedback')} />
                                </Button>
                                <Button variant="ghost_icon" onClick={() => navigate(`/info`)}>
                                    <Icon name={Icons.Info} size={24} title="Info Page" />
                                </Button>
                                <Button variant="ghost_icon" onClick={() => navigate(`/`)}>
                                    <Icon name={Icons.Platform} size={24} title="Change Asset" />
                                </Button>
                                {installation && <AlertIcon installation={installation} />}
                            </IconStyle>
                            <SelectLanguageWrapper>
                                <SelectLanguage />
                            </SelectLanguageWrapper>
                        </EdsProvider>
                    </TopBar.Actions>
                </StyledTopBar>
            </TopBarBackground>
            {banner && <AlertBanner dismissAlert={clearBanner} bannerAlert={banner} />}
            {isFeedbackOpen && <FeedbackDialog isOpen onClose={() => setIsFeedbackOpen(false)} />}
        </>
    )
}
