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
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
}

const StyledStatus = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 2px;
`
const LongTypography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize}px;
    overflow: visible;
    white-space: normal;
    text-overflow: unset;
    word-break: break-word;
    hyphens: auto;
`

export const RobotStatusChip = ({ status, flotillaStatus, isarConnected, itemSize }: StatusProps) => {
    const { TranslateText } = useLanguageContext()

    let iconColor = tokens.colors.text.static_icons__default.hex
    let statusIcon = Icons.CloudOff
    switch (status) {
        case RobotStatus.Home:
        case RobotStatus.ReturningHome:
        case RobotStatus.Available: {
            statusIcon = Icons.Successful
            iconColor = tokens.colors.interactive.success__resting.hex
            break
        }
        case RobotStatus.UnkownStatus:
        case RobotStatus.Busy: {
            statusIcon = Icons.Ongoing
            iconColor = tokens.colors.text.static_icons__default.hex
            break
        }
        case RobotStatus.Blocked:
        case RobotStatus.BlockedProtectiveStop: {
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
    } else if (flotillaStatus && status === RobotStatus.Available && flotillaStatus === RobotFlotillaStatus.Home) {
        iconColor = tokens.colors.interactive.danger__resting.hex
        statusIcon = Icons.Warning
        status = RobotStatus.Home
    } else if (
        flotillaStatus &&
        status === RobotStatus.Available &&
        flotillaStatus === RobotFlotillaStatus.Recharging
    ) {
        iconColor = '#FFC300'
        statusIcon = Icons.BatteryCharging
        status = RobotStatus.Recharging
    }

    return (
        <StyledStatus>
            <Icon name={statusIcon} size={itemSize} style={{ color: iconColor }} />
            <LongTypography $fontSize={itemSize} variant="body_short">
                {TranslateText(status)}
            </LongTypography>
        </StyledStatus>
    )
}
