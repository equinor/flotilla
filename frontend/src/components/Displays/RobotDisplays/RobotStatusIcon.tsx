import { Icon, Typography } from '@equinor/eds-core-react'
import { RobotStatus } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useSafeZoneContext } from 'components/Contexts/SafeZoneContext'
import { Icons } from 'utils/icons'
import { styled } from 'styled-components'

interface StatusProps {
    status?: RobotStatus
    isarConnected: boolean
}

const StyledStatus = styled.div`
    display: flex;
    flex-direction: row;
    align-items: end;
    gap: 2px;
`

const StyledIcon = styled(Icon)`
    width: 24px;
    height: 24px;
`

export const RobotStatusChip = ({ status, isarConnected }: StatusProps) => {
    const { TranslateText } = useLanguageContext()
    const { safeZoneStatus } = useSafeZoneContext()

    var iconColor = tokens.colors.text.static_icons__default.hex
    var statusIcon = Icons.CloudOff
    switch (status) {
        case RobotStatus.Available: {
            statusIcon = Icons.Successful
            iconColor = tokens.colors.interactive.success__resting.hex
            break
        }
        case RobotStatus.Busy: {
            statusIcon = Icons.ClosedCircleOutlined
            iconColor = tokens.colors.interactive.warning__resting.hex
            break
        }
        case RobotStatus.Blocked: {
            statusIcon = Icons.Blocked
            iconColor = tokens.colors.interactive.danger__resting.hex
            break
        }
        default: {
            iconColor = tokens.colors.text.static_icons__default.hex
            statusIcon = Icons.CloudOff
            status = RobotStatus.Offline
            break
        }
    }

    if (!isarConnected) {
        iconColor = tokens.colors.interactive.disabled__text.hex
        statusIcon = Icons.Info
        status = RobotStatus.ConnectionIssues
    } else if (safeZoneStatus) {
        iconColor = tokens.colors.interactive.danger__resting.hex
        statusIcon = Icons.Warning
        status = RobotStatus.SafeZone
    }

    return (
        <StyledStatus>
            <StyledIcon name={statusIcon} style={{ color: iconColor }} />
            <Typography variant="body_short">{TranslateText(status)}</Typography>
        </StyledStatus>
    )
}
