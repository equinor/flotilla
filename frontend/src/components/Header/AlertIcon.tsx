import { Button, Icon, Popover, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useRef, useState } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { AlertListItem } from 'components/Alerts/AlertsListItem'
import { useAssetContext } from 'components/Contexts/AssetContext'

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

export const AlertIcon = () => {
    const { listAlerts } = useAlertContext()
    const { installationName } = useAssetContext()
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
            <StyledPopover
                onClose={onAlertClose}
                open={isAlertDialogOpen}
                placement={'bottom-end'}
                anchorEl={referenceElementNotifications.current}
            >
                <StyledAlertPopoverHeader>
                    <Typography variant="h4">{TranslateText('Alerts')}</Typography>
                    <Button variant={'ghost_icon'} style={{ color: 'black' }} onClick={onAlertClose}>
                        <Icon name="close" size={24} />
                    </Button>
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
            </StyledPopover>
        </>
    )
}
