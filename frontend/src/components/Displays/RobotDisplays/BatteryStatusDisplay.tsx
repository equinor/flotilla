import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { BatteryStatus } from 'models/Battery'

const BatteryAlignment = styled.div`
    display: flex;
`

const StyledTypography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize}px;
`
interface BatteryStatusDisplayProps {
    batteryLevel?: number
    batteryState?: BatteryStatus
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
    textAlignedBottom?: boolean
}

export const BatteryStatusDisplay = ({
    batteryLevel,
    batteryState,
    itemSize,
    textAlignedBottom,
}: BatteryStatusDisplayProps) => {
    let iconColor: string = tokens.colors.text.static_icons__default.hex

    const getBatteryIcon = (batteryLevel?: number) => {
        switch (true) {
            case batteryState === BatteryStatus.Charging:
                return Icons.BatteryCharging
            case batteryState === BatteryStatus.Critical:
            case batteryState === BatteryStatus.Error:
                return Icons.BatteryAlert
            case batteryState == BatteryStatus.Normal:
                return Icons.Battery
            case batteryLevel == null:
            default:
                return Icons.BatteryUnknown
        }
    }

    const batteryIcon =
        batteryLevel !== null && batteryLevel !== undefined ? getBatteryIcon(batteryLevel) : Icons.BatteryUnknown

    const batteryValue = batteryLevel !== null && batteryLevel !== undefined ? `${Math.round(batteryLevel)}%` : '---%'

    iconColor = batteryIcon === Icons.BatteryAlert ? tokens.colors.interactive.warning__resting.hex : iconColor

    return (
        <BatteryAlignment style={{ alignItems: textAlignedBottom ? 'end' : 'center' }}>
            {batteryIcon && <Icon name={batteryIcon} color={iconColor} size={itemSize} />}
            <StyledTypography $fontSize={itemSize}>{batteryValue}</StyledTypography>
        </BatteryAlignment>
    )
}
