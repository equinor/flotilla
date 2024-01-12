import { config } from 'config'
import { Button, Icon, TopBar, Typography } from '@equinor/eds-core-react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import styled from 'styled-components'
import { SelectLanguage } from './LanguageSelector'
import { Icons } from 'utils/icons'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { AlertBanner } from 'components/Alerts/AlertsBanner'

const StyledTopBar = styled(TopBar)`
    margin-bottom: 2rem;
    align-items: center;
    box-shadow: none;

    @media (max-width: 700px) {
        display: flex;
        justify-self: start;
        flex-direction: column;
        height: 90px;
    }
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
    > * {
        margin-left: 1rem;
    }
`
const HandPointer = styled.div`
    cursor: pointer;
`
const SelectLanguageWrapper = styled.div`
    margin-left: 1.5rem;
`
const StyledAlertList = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 0.5rem;
`
export const Header = ({ page }: { page: string }) => {
    const { alerts } = useAlertContext()
    const { installationName } = useInstallationContext()
    return (
        <>
            <StyledTopBar>
                <StyledWrapper>
                    <HandPointer>
                        <TopBar.Header
                            onClick={() => {
                                window.location.href = `${config.FRONTEND_BASE_ROUTE}/FrontPage`
                            }}
                        >
                            <Typography variant="body_long_bold" color="primary">
                                Flotilla
                            </Typography>
                        </TopBar.Header>
                    </HandPointer>
                    <Typography> {installationName}</Typography>
                </StyledWrapper>
                <TopBar.Actions>
                    <IconStyle>
                        <Button variant="ghost_icon" onClick={() => console.log('Clicked account icon')}>
                            <Icon name={Icons.Account} size={24} title="user" />
                        </Button>
                        <Button variant="ghost_icon" onClick={() => console.log('Clicked accessibility icon')}>
                            <Icon name={Icons.Accessible} size={24} />
                        </Button>
                        <Button variant="ghost_icon" onClick={() => console.log('Clicked notification icon')}>
                            <Icon name={Icons.Notifications} size={24} />
                        </Button>
                        <Button
                            variant="ghost_icon"
                            onClick={() => {
                                window.location.href = `${config.FRONTEND_BASE_ROUTE}/`
                            }}
                        >
                            <Icon name={Icons.Platform} size={24} title="Change Asset" />
                        </Button>
                    </IconStyle>
                    <SelectLanguageWrapper>{SelectLanguage()}</SelectLanguageWrapper>
                </TopBar.Actions>
            </StyledTopBar>
            {Object.entries(alerts).length > 0 && (
                <StyledAlertList>
                    {Object.entries(alerts).map(([key, value]) => (
                        <AlertBanner key={key} dismissAlert={value.dismissFunction}>
                            {value.content}
                        </AlertBanner>
                    ))}
                </StyledAlertList>
            )}
        </>
    )
}
