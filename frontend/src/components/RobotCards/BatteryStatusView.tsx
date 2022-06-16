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
    battery: Battery
}

const BatteryStatusView = ({ battery }: BatteryStatusViewProps): JSX.Element => {
    let battery_icon
    let icon_color: string = tokens.colors.interactive.primary__resting.hex
    let battery_value: string = `${battery.value}%`

    if (!battery.value) battery_value = '---%'

    if (battery.status === BatteryStatus.Normal) {
        battery_icon = 'battery'
    } else if (battery.status === BatteryStatus.Charging) {
        battery_icon = 'battery_charging'
    } else if (battery.status === BatteryStatus.Critical) {
        battery_icon = 'battery_alert'
        icon_color = tokens.colors.interactive.danger__resting.hex
    } else if (battery.status === BatteryStatus.Error) {
        battery_icon = 'battery_unknown'
    }

    return (
        <BatteryStatusTile>
            <Typography>{battery_value}</Typography>
            <Icon name={battery_icon} color={icon_color} size={24} />
        </BatteryStatusTile>
    )
}

export default BatteryStatusView
