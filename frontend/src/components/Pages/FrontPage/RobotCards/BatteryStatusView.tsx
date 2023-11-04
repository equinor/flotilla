import { BatteryStatus } from 'models/Battery'
import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { RobotStatus } from 'models/Robot'

const BatteryAlignment = styled.div`
    display: flex;
    align-items: end;
`

const StyledTopography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize};
`
export interface BatteryStatusViewProps {
    battery?: number
    batteryStatus?: BatteryStatus
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
    robotStatus: RobotStatus
}

const BatteryStatusView = ({
    robotStatus,
    battery,
    batteryStatus,
    itemSize = 24,
}: BatteryStatusViewProps): JSX.Element => {
    let battery_icon
    let icon_color: string = tokens.colors.interactive.primary__resting.hex
    let battery_value: string

    if (!battery) {
        battery_value = '---%'
        battery_icon = Icons.BatteryUnknown
    } else if (robotStatus === RobotStatus.Offline) {
        battery_value = ''
        icon_color = tokens.colors.interactive.disabled__text.hex
        battery_icon = Icons.BatteryUnknown
    } else {
        battery_value = `${battery}%`
        switch (batteryStatus) {
            case BatteryStatus.Normal:
                battery_icon = Icons.Battery
                break
            case BatteryStatus.Charging:
                battery_icon = Icons.BatteryCharging
                break
            case BatteryStatus.Critical:
                battery_icon = Icons.BatteryAlert
                icon_color = tokens.colors.interactive.danger__resting.hex
                break
            default:
                battery_icon = Icons.BatteryUnknown
                battery_value = '---%'
                break
        }
    }

    return (
        <BatteryAlignment>
            <Icon name={battery_icon} color={icon_color} size={itemSize} />
            <StyledTopography $fontSize={itemSize} style={{ color: tokens.colors.text.static_icons__tertiary.hex }}>
                {battery_value}
            </StyledTopography>
        </BatteryAlignment>
    )
}

export default BatteryStatusView
