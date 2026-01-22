import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const PressureAlignment = styled.div`
    display: flex;
    gap: 7px;
    align-items: center;
`
const StyledTypography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize}px;
`
interface PressureStatusDisplayProps {
    pressure: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
}

export const PressureStatusDisplay = ({ pressure, itemSize }: PressureStatusDisplayProps) => {
    const barToMillibar = 1000
    const pressureInMilliBar = `${Math.round(pressure * barToMillibar)}mBar`

    return (
        <PressureAlignment>
            <Icon name={Icons.Pressure} color={tokens.colors.text.static_icons__default.hex} size={itemSize} />
            <StyledTypography $fontSize={itemSize}>{pressureInMilliBar}</StyledTypography>
        </PressureAlignment>
    )
}
