import { Button, Icon, Popover, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useAlertContext } from 'contexts/AlertContext'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useState } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { useNavigate } from 'react-router'
import { AlertNotification } from 'components/Alerts/AlertNotification'
import { Installation } from 'models/Installation'

const Circle = styled.div`
    position: absolute;
    margin: 14px 23px 0px;
    width: 9px;
    height: 9px;
    border-radius: 50%;
`

const Gaps = styled.div`
    display: flex;
    flex-direction: column;
    gap: 10px;
`

interface Props {
    installation: Installation
}
export const AlertIcon = ({ installation }: Props) => {
    const { notifications, removeNotification } = useAlertContext()
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

    const onNotificationClose = (index: number) => {
        removeNotification(index)
    }

    const handleMissionClick = (missionId: string) => {
        navigate(`/${installation.installationCode}/mission/${missionId}`)
        onAlertClose()
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
            <Popover
                onClose={onAlertClose}
                open={isAlertDialogOpen}
                placement={'bottom-end'}
                anchorEl={referenceElementNotifications}
            >
                <Popover.Header>
                    <Typography variant="h4">{TranslateText('Alerts')}</Typography>
                </Popover.Header>
                <Popover.Content>
                    {notifications.length === 0 && <Typography variant="h6">{TranslateText('No alerts')}</Typography>}
                    {notifications.length > 0 && (
                        <Gaps>
                            {notifications.map((notification, index) => (
                                <AlertNotification
                                    key={index}
                                    notification={notification}
                                    onCloseClick={() => onNotificationClose(index)}
                                    onMissionClick={handleMissionClick}
                                />
                            ))}
                        </Gaps>
                    )}
                </Popover.Content>
            </Popover>
        </>
    )
}
