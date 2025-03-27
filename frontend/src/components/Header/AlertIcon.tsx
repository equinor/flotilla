import { Button, Icon, Popover, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useRef, useState } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { AlertListItem } from 'components/Alerts/AlertsListItem'

const Circle = styled.div`
    position: absolute;
    margin: 14px 23px 0px;
    width: 9px;
    height: 9px;
    border-radius: 50%;
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
const StyledAlertList = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 0.5rem;
`

export const AlertIcon = () => {
    const { listAlerts } = useAlertContext()
    const { installationName } = useInstallationContext()
    const { TranslateText } = useLanguageContext()
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
            <Button
                variant="ghost_icon"
                onClick={!isAlertDialogOpen ? onAlertOpen : onAlertClose}
                ref={referenceElementNotifications}
            >
                <Icon name={Icons.Notifications} size={24} />
                {Object.entries(listAlerts).length !== 0 &&
                    installationName && ( //Alert banners
                        <Circle style={{ background: tokens.colors.interactive.danger__resting.hex }} />
                    )}
            </Button>
            <Popover
                onClose={onAlertClose}
                open={isAlertDialogOpen}
                placement={'bottom'}
                anchorEl={referenceElementNotifications.current}
            >
                <StyledAlertPopoverHeader>
                    <StyledAlertPopoverTitle>
                        <Typography variant="h6">{TranslateText('Alerts')}</Typography>
                        <Button variant={'ghost_icon'} onClick={onAlertClose}>
                            <Icon name="close" size={24} />
                        </Button>
                    </StyledAlertPopoverTitle>
                </StyledAlertPopoverHeader>
                <Popover.Content>
                    {Object.entries(listAlerts).length === 0 && installationName && (
                        <Typography variant="h6">{TranslateText('No alerts')}</Typography>
                    )}
                    {Object.entries(listAlerts).length > 0 && installationName && (
                        <StyledAlertList>
                            {Object.entries(listAlerts).map(([key, value]) => (
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
        </>
    )
}
