import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { Alert } from 'models/Alert'

const NotificationBox = styled.div<{ $clickable: boolean }>`
    width: 20rem;
    padding: 10px;
    border-radius: 6px;
    border: 1px solid lightgray;
    cursor: ${(props) => (props.$clickable ? 'pointer' : 'auto')};
`

const SpaceBetween = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
`

const SmallButton = styled(Button)`
    width: 20px;
    height: 20px;
    &::after {
        display: none;
    }
`

const Horizontal = styled.div`
    display: flex;
    flex-direction: row;
    gap: 0.5rem;
    align-items: center;
`

interface Props {
    notification: Alert
    onMissionClick: (missionId: string) => void
    onCloseClick: () => void
}

export const AlertNotification = ({ notification, onMissionClick, onCloseClick }: Props) => {
    let iconColor = tokens.colors.interactive.danger__resting.hex
    if (notification.severity === 'warning') {
        iconColor = tokens.colors.interactive.warning__resting.hex
    }
    if (notification.severity === 'info') {
        iconColor = tokens.colors.text.static_icons__default.hex
    }

    return (
        <NotificationBox
            $clickable={notification.missionId !== undefined}
            onClick={() => {
                if (notification.missionId) {
                    onMissionClick(notification.missionId)
                }
            }}
        >
            <SpaceBetween>
                <div>
                    <Horizontal>
                        <Icon name={Icons.Failed} style={{ color: iconColor }} />
                        <Typography variant="body_short" style={{ fontWeight: 500 }}>
                            {notification.title}
                        </Typography>
                    </Horizontal>
                    {notification.message && <Typography variant="body_short">{notification.message}</Typography>}
                </div>
                <SmallButton
                    variant="ghost_icon"
                    onClick={(e) => {
                        e.stopPropagation() // Ensure that the parent onClick is not triggered
                        onCloseClick()
                    }}
                >
                    <Icon name={Icons.Clear} />
                </SmallButton>
            </SpaceBetween>
        </NotificationBox>
    )
}
