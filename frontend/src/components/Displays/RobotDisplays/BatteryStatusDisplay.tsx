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
export interface BatteryStatusDisplayProps {
    batteryLevel?: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
}

export const BatteryStatusDisplay = ({ batteryLevel, itemSize }: BatteryStatusDisplayProps): JSX.Element => {
    let iconColor: string = tokens.colors.interactive.primary__resting.hex

    const getBatteryIcon = (batteryLevel: number) => {
        switch (true) {
            case !batteryLevel:
                return Icons.BatteryUnknown
            case batteryLevel > 10:
                return Icons.Battery
            case batteryLevel <= 10:
                return Icons.BatteryAlert
            default:
                return Icons.BatteryUnknown
        }
    }

    const batteryIcon = batteryLevel ? getBatteryIcon(batteryLevel) : Icons.BatteryUnknown

    const batteryValue = batteryLevel ? `${batteryLevel}%` : '---%'

    iconColor = batteryIcon === Icons.BatteryAlert ? tokens.colors.interactive.danger__resting.hex : iconColor

    return (
        <BatteryAlignment>
            <Icon name={batteryIcon} color={iconColor} size={itemSize} />
            <StyledTypography $fontSize={itemSize}>{batteryValue}</StyledTypography>
        </BatteryAlignment>
    )
}
