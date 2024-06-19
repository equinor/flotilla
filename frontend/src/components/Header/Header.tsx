import { config } from 'config'
import { Button, Icon, TopBar, Typography, Popover } from '@equinor/eds-core-react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import styled from 'styled-components'
import { SelectLanguage } from './LanguageSelector'
import { Icons } from 'utils/icons'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { AlertBanner } from 'components/Alerts/AlertsBanner'
import { AlertListItem } from 'components/Alerts/AlertsListItem'
import { useState, useRef } from 'react'
import { tokens } from '@equinor/eds-tokens'

const StyledTopBar = styled(TopBar)`
    align-items: center;
    box-shadow: none;
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
const StyledAlertPopoverHeader = styled(Popover.Header)`
    width: 350px;
`
const StyledAlertPopoverTitle = styled(Popover.Title)`
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-right: 0.25em !important;
`
const Circle = styled.div`
    position: absolute;
    margin: 14px 23px 0px;
    width: 9px;
    height: 9px;
    border-radius: 50%;
`
export const Header = ({ page }: { page: string }) => {
    const { alerts } = useAlertContext()
    const { installationName } = useInstallationContext()

    const [isAlertDialogOpen, setIsAlertDialogOpen] = useState<boolean>(false)
    const referenceElementNotifications = useRef<HTMLButtonElement>(null)

    const onAlertOpen = () => {
        setIsAlertDialogOpen(true)
    }

    const onAlertClose = () => {
        setIsAlertDialogOpen(false)
    }

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
                        <Button
                            variant="ghost_icon"
                            onClick={!isAlertDialogOpen ? onAlertOpen : onAlertClose}
                            ref={referenceElementNotifications}
                        >
                            <Icon name={Icons.Notifications} size={24} />
                            {Object.entries(alerts).length !== 0 &&
                                installationName &&
                                page !== 'root' && ( //Alert banners
                                    <Circle style={{ background: tokens.colors.interactive.danger__resting.hex }} />
                                )}
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
            <Popover
                onClose={onAlertClose}
                open={isAlertDialogOpen}
                placement={'bottom'}
                anchorEl={referenceElementNotifications.current}
            >
                <StyledAlertPopoverHeader>
                    <StyledAlertPopoverTitle>
                        <span>System alerts</span>
                        <Button variant={'ghost_icon'} onClick={onAlertClose}>
                            <Icon name="close" size={24} />
                        </Button>
                    </StyledAlertPopoverTitle>
                </StyledAlertPopoverHeader>
                <Popover.Content>
                    {Object.entries(alerts).length === 0 && installationName && page !== 'root' && (
                        <Typography variant="h6">No alerts</Typography>
                    )}
                    {Object.entries(alerts).length > 0 && installationName && page !== 'root' && (
                        <StyledAlertList>
                            {Object.entries(alerts).map(([key, value]) => (
                                <AlertListItem
                                    key={key}
                                    dismissAlert={value.dismissFunction}
                                    alertCategory={value.alertCategory}
                                >
                                    {value.content}
                                </AlertListItem>
                            ))}
                        </StyledAlertList>
                    )}
                </Popover.Content>
            </Popover>
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
