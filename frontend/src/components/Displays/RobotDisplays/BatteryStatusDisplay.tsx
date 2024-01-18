import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const BatteryAlignment = styled.div`
    display: flex;
    align-items: end;
`

const StyledTypography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize};
`
interface BatteryStatusDisplayProps {
    batteryLevel?: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
    batteryWarningLimit?: number
}

export const BatteryStatusDisplay = ({
    batteryLevel,
    itemSize,
    batteryWarningLimit,
}: BatteryStatusDisplayProps): JSX.Element => {
    let iconColor: string = tokens.colors.interactive.primary__resting.hex

    const getBatteryIcon = (batteryLevel: number) => {
        switch (true) {
            case !batteryLevel:
                return Icons.BatteryUnknown
            case !batteryWarningLimit || batteryLevel > batteryWarningLimit:
                return Icons.Battery
            case batteryWarningLimit && batteryLevel <= batteryWarningLimit:
                return Icons.BatteryAlert
            default:
                return Icons.BatteryUnknown
        }
    }

    const batteryIcon = batteryLevel ? getBatteryIcon(batteryLevel) : Icons.BatteryUnknown

    const batteryValue = batteryLevel ? `${batteryLevel}%` : '---%'

    iconColor = batteryIcon === Icons.BatteryAlert ? tokens.colors.interactive.warning__resting.hex : iconColor

    return (
        <BatteryAlignment>
            <Icon name={batteryIcon} color={iconColor} size={itemSize} />
            <StyledTypography $fontSize={itemSize}>{batteryValue}</StyledTypography>
        </BatteryAlignment>
    )
}
