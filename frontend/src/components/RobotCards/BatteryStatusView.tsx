import { Battery, BatteryStatus } from 'models/battery'
import { battery, battery_charging, battery_alert, battery_unknown } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'

import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'

Icon.add({ battery, battery_charging, battery_unknown, battery_alert })

const BatteryStatusTile = styled.div`
    display: flex;
    align-items: center;
    justify-content: flex-end;
`

export interface BatteryStatusViewProps {
    battery?: number
    batteryStatus?: BatteryStatus
}

const BatteryStatusView = ({ battery, batteryStatus }: BatteryStatusViewProps): JSX.Element => {
    let battery_icon
    let icon_color: string = tokens.colors.interactive.primary__resting.hex
    let battery_value: string

    if (!battery) {
        battery_value = '---%'
        battery_icon = 'battery_unknown'
    } else {
        battery_value = `${battery}%`
        switch (batteryStatus) {
            case BatteryStatus.Normal:
                battery_icon = 'battery'
                break
            case BatteryStatus.Charging:
                battery_icon = 'battery_charging'
                break
            case BatteryStatus.Critical:
                battery_icon = 'battery_alert'
                icon_color = tokens.colors.interactive.danger__resting.hex
                break
            default:
                battery_icon = 'battery_unknown'
                battery_value = '---%'
                break
        }
    }

    return (
        <BatteryStatusTile>
            <Typography>{battery_value}</Typography>
            <Icon name={battery_icon} color={icon_color} size={24} />
        </BatteryStatusTile>
    )
}

export default BatteryStatusView
