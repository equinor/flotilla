import { Icon, Typography } from '@equinor/eds-core-react'
import { RobotFlotillaStatus, RobotStatus } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { styled } from 'styled-components'

interface StatusProps {
    status?: RobotStatus
    isarConnected: boolean
    flotillaStatus?: RobotFlotillaStatus
}

const StyledStatus = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 2px;
`

const LongTypography = styled(Typography)`
    overflow: visible;
    white-space: normal;
    text-overflow: unset;
    word-break: break-word;
    hyphens: auto;
`

const StyledIcon = styled(Icon)`
    width: 24px;
    min-width: 24px;
    height: 24px;
`

export const RobotStatusChip = ({ status, flotillaStatus, isarConnected }: StatusProps) => {
    const { TranslateText } = useLanguageContext()

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
    } else if (flotillaStatus && flotillaStatus === RobotFlotillaStatus.SafeZone) {
        iconColor = tokens.colors.interactive.danger__resting.hex
        statusIcon = Icons.Warning
        status = RobotStatus.SafeZone
    } else if (flotillaStatus && flotillaStatus === RobotFlotillaStatus.Recharging) {
        iconColor = '#FFC300'
        statusIcon = Icons.BatteryCharging
        status = RobotStatus.Recharging
    }

    return (
        <StyledStatus>
            <StyledIcon name={statusIcon} style={{ color: iconColor }} />
            <LongTypography variant="body_short">{TranslateText(status)}</LongTypography>
        </StyledStatus>
    )
}
