import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { PressureStatus } from 'models/Pressure'

const PressureAlignment = styled.div`
    display: flex;
    align-items: center;
    gap: 7px;
`

const StyledTypography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize};
`
interface PressureStatusDisplayProps {
    pressureInBar: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
    upperPressureWarningThreshold?: number
    lowerPressureWarningThreshold?: number
}

export const PressureStatusDisplay = ({
    pressureInBar,
    itemSize,
    upperPressureWarningThreshold,
    lowerPressureWarningThreshold,
}: PressureStatusDisplayProps): JSX.Element => {
    const barToMillibar = 1000
    const pressureInMilliBar = `${Math.round(pressureInBar * barToMillibar)}mBar`
    let icon_color: string = tokens.colors.interactive.primary__resting.hex
    let pressureStatus: PressureStatus

    if (!upperPressureWarningThreshold || !lowerPressureWarningThreshold) {
        pressureStatus = PressureStatus.Normal
    } else if (
        pressureInBar * barToMillibar > upperPressureWarningThreshold ||
        pressureInBar * barToMillibar < lowerPressureWarningThreshold
    ) {
        pressureStatus = PressureStatus.Critical
    } else {
        pressureStatus = PressureStatus.Normal
    }

    switch (pressureStatus) {
        case PressureStatus.Normal:
            icon_color = tokens.colors.interactive.primary__resting.hex
            break
        case PressureStatus.Critical:
            icon_color = tokens.colors.interactive.warning__resting.hex
            break
        default:
            icon_color = tokens.colors.interactive.disabled__text.hex
            break
    }

    return (
        <PressureAlignment>
            <Icon name={Icons.Pressure} color={icon_color} size={itemSize} />
            <StyledTypography $fontSize={itemSize}>{pressureInMilliBar}</StyledTypography>
        </PressureAlignment>
    )
}
