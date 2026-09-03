import { Button, Icon, Popover, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useAlertContext } from 'contexts/AlertContext'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useState } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { useNavigate } from 'react-router'

const Circle = styled.div`
    position: absolute;
    margin: 14px 23px 0px;
    width: 9px;
    height: 9px;
    border-radius: 50%;
`
const StyledAlertPopoverHeader = styled.div`
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 16px 0px 16px;
    margin-bottom: -10px;
    width: 340px;
`

const StyledAlertList = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 15px;
`

const StyledPopover = styled(Popover)`
    width: 360px;
    border-radius: 6px;
`

const StyledNotificationItem = styled.div<{ severity: string }>`
    width: 330px;
    border-radius: 6px;
    border: 1px solid lightgray;
    display: flex;
    flex-direction: column;
    gap: 10px;
    padding: 10px;
    background-color: ${(props) => {
        switch (props.severity) {
            case 'error':
                return tokens.colors.ui.background__danger.hex
            case 'warning':
                return tokens.colors.interactive.warning__highlight.hex
            case 'info':
                return tokens.colors.infographic.primary__mist_blue.hex
            default:
                return tokens.colors.ui.background__light.hex
        }
    }};
    opacity: 0.1;
    cursor: pointer;
`

const NotificationContent = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 10px;
`

const ClearAllButton = styled(Button)`
    width: 100%;
    margin-top: 10px;
`

export const AlertIcon = () => {
    const { notifications, removeNotification, clearAllNotifications } = useAlertContext()
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const [isAlertDialogOpen, setIsAlertDialogOpen] = useState<boolean>(false)

    const [referenceElementNotifications, setReferenceElementNotifications] = useState<HTMLButtonElement | null>(null)

    const onAlertOpen = () => {
        setIsAlertDialogOpen(true)
    }

    const onAlertClose = () => {
        setIsAlertDialogOpen(false)
    }

    const handleNotificationClick = (notification: any) => {
        // Navigate to mission if missionId is present
        if (notification.missionId) {
            navigate(`/mission/${notification.missionId}`)
            onAlertClose()
        }
    }

    return (
        <>
            <Button
                variant="ghost_icon"
                onClick={!isAlertDialogOpen ? onAlertOpen : onAlertClose}
                ref={setReferenceElementNotifications}
            >
                <Icon name={Icons.Notifications} size={24} />
                {notifications.length > 0 && (
                    <Circle style={{ background: tokens.colors.interactive.danger__resting.hex }} />
                )}
            </Button>
            <StyledPopover
                onClose={onAlertClose}
                open={isAlertDialogOpen}
                placement={'bottom-end'}
                anchorEl={referenceElementNotifications}
            >
                <StyledAlertPopoverHeader>
                    <Typography variant="h4">{TranslateText('Alerts')}</Typography>
                    <Button variant={'ghost_icon'} style={{ color: 'black' }} onClick={onAlertClose}>
                        <Icon name="close" size={24} />
                    </Button>
                </StyledAlertPopoverHeader>
                <Popover.Content>
                    {notifications.length === 0 && <Typography variant="h6">{TranslateText('No alerts')}</Typography>}
                    {notifications.length > 0 && (
                        <>
                            <StyledAlertList>
                                {notifications.map((notification, index) => (
                                    <NotificationContent key={index}>
                                        <StyledNotificationItem
                                            severity={notification.severity}
                                            onClick={() => handleNotificationClick(notification)}
                                        >
                                            <Typography variant="body_short" style={{ fontWeight: 500 }}>
                                                {notification.title}
                                            </Typography>
                                            {notification.message && (
                                                <Typography variant="body_short">{notification.message}</Typography>
                                            )}
                                        </StyledNotificationItem>
                                        <Button
                                            variant="ghost_icon"
                                            onClick={() => removeNotification(index)}
                                            style={{ alignSelf: 'flex-start', marginTop: '8px' }}
                                        >
                                            <Icon name={Icons.Clear} size={18} />
                                        </Button>
                                    </NotificationContent>
                                ))}
                            </StyledAlertList>
                            <ClearAllButton variant="ghost" onClick={clearAllNotifications}>
                                {TranslateText('Clear all')}
                            </ClearAllButton>
                        </>
                    )}
                </Popover.Content>
            </StyledPopover>
        </>
    )
}
