import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const PressureStatusTile = styled.div`
    display: flex;
    align-items: center;
    justify-content: flex-end;
`

export interface PressureStatusViewProps {
    pressure?: number
}

const PressureStatusView = ({ pressure }: PressureStatusViewProps): JSX.Element => {
    const barToMillibar = 1000
    let icon_color: string = tokens.colors.interactive.primary__resting.hex

    if (!pressure) {
        return <></>
    }
    return (
        <PressureStatusTile>
            <Typography>{`${Math.round(pressure * barToMillibar)}mBar`}</Typography>
            <Icon name={Icons.Pressure} color={icon_color} size={24} />
        </PressureStatusTile>
    )
}

export default PressureStatusView
