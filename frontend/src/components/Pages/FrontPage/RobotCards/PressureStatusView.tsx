import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const PressureAlignment = styled.div`
    display: flex;
    align-items: center;
`

const StyledTopography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize};
`
export interface PressureStatusViewProps {
    pressure?: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
}

const PressureStatusView = ({ pressure, itemSize }: PressureStatusViewProps): JSX.Element => {
    const barToMillibar = 1000
    let icon_color: string = tokens.colors.interactive.primary__resting.hex

    if (!pressure) {
        return <></>
    }
    return (
        <PressureAlignment>
            <Icon name={Icons.Pressure} color={icon_color} size={itemSize} />
            <StyledTopography $fontSize={itemSize}> {`${Math.round(pressure * barToMillibar)}mBar`}</StyledTopography>
        </PressureAlignment>
    )
}

export default PressureStatusView
