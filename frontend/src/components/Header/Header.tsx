import { config } from 'config'
import { Button, Icon, TopBar, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { SelectLanguage } from 'components/Header/LanguageSelector'
import { Icons } from 'utils/icons'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { AlertBanner } from 'components/Alerts/AlertsBanner'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { NavigationMenu } from 'components/NavigationMenu/NavigationMenu'
import { findNavigationPage } from 'pages/AssetSelectionPage/AssetSelectionPage'
import { AlertIcon } from 'components/Header/AlertIcon'
import { useNavigate } from 'react-router-dom'
import { phone_width } from 'utils/constants'
import { useAssetContext } from 'components/Contexts/AssetContext'

const StyledTopBar = styled(TopBar)`
    align-items: center;
    box-shadow: none;
    @media (max-width: ${phone_width}) {
        grid-column-gap: 12px;
    }
    height: fit-content;
`
const StyledWrapper = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
`
const IconStyle = styled.div`
    display: flex;
    align-items: center;
    flex-direction: row-reverse;
    gap: 0.8rem;
`
const HandPointer = styled.div`
    cursor: pointer;
`
const SelectLanguageWrapper = styled.div`
    margin-left: 1rem;
`
const StyledAlertList = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 0.5rem;
`
const StyledTopBarHeader = styled.div`
    display: flex;
    flex-direction: row;
    gap: 4px;
`
const StyledNavigationMenu = styled.div`
    display: none;
    @media (max-width: ${phone_width}) {
        display: block;
    }
`

export const Header = ({ page }: { page: string }) => {
    const { alerts } = useAlertContext()
    const { installationName, installationCode } = useAssetContext()
    const navigate = useNavigate()

    return (
        <>
            <StyledTopBar id={FrontPageSectionId.TopBar}>
                <StyledWrapper>
                    <TopBar.Header onClick={() => navigate(findNavigationPage(installationCode))}>
                        <StyledTopBarHeader>
                            <HandPointer>
                                <Typography variant="body_short_bold" color="text-primary">
                                    Flotilla
                                </Typography>
                            </HandPointer>
                            <Typography variant="body_short" color="text-primary">
                                {`| ${installationName}`}
                            </Typography>
                        </StyledTopBarHeader>
                    </TopBar.Header>
                </StyledWrapper>
                <TopBar.Actions>
                    <IconStyle>
                        {page !== 'root' && <AlertIcon />}
                        <Button variant="ghost_icon" onClick={() => navigate(`${config.FRONTEND_BASE_ROUTE}/`)}>
                            <Icon name={Icons.Platform} size={24} title="Change Asset" />
                        </Button>
                        <Button variant="ghost_icon" onClick={() => navigate(`${config.FRONTEND_BASE_ROUTE}/info`)}>
                            <Icon name={Icons.Info} size={24} title="Info Page" />
                        </Button>
                    </IconStyle>
                    <SelectLanguageWrapper>
                        <SelectLanguage />
                    </SelectLanguageWrapper>
                </TopBar.Actions>
            </StyledTopBar>
            <StyledNavigationMenu>
                <NavigationMenu />
            </StyledNavigationMenu>
            {Object.entries(alerts).length > 0 && installationName && page !== 'root' && (
                <StyledAlertList>
                    {Object.entries(alerts).map(([key, value]) => (
                        <AlertBanner key={key} dismissAlert={value.dismissFunction} alertCategory={value.alertCategory}>
                            {value.content}
                        </AlertBanner>
                    ))}
                </StyledAlertList>
            )}
        </>
    )
}
