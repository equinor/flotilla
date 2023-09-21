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

export function Header({ page }: { page: string }) {
    const { alerts } = useAlertContext()
    const { installationName } = useInstallationContext()
    return (
        <>
            <StyledTopBar>
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
            {Object.entries(alerts).length > 0 &&
                Object.entries(alerts).map(([key, value]) => (
                    <AlertBanner key={key} dismissAlert={value.dismissFunction}>{value.content}</AlertBanner>
                ))}
        </>
    )
}
