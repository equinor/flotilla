import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { PressureStatus } from 'models/Pressure'

const PressureAlignment = styled.div`
    display: flex;
    align-items: center;
`

const StyledTopography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize};
`
export interface PressureStatusViewProps {
    pressureInBar?: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
    upperPressureWarningThreshold?: number
    lowerPressureWarningThreshold?: number
}

const PressureStatusView = ({
    pressureInBar,
    itemSize,
    upperPressureWarningThreshold,
    lowerPressureWarningThreshold,
}: PressureStatusViewProps): JSX.Element => {
    const barToMillibar = 1000
    let icon_color: string = tokens.colors.interactive.primary__resting.hex
    let pressureStatus: PressureStatus

    if (!pressureInBar) {
        return <></>
    } else if (!upperPressureWarningThreshold || !lowerPressureWarningThreshold) {
        pressureStatus = PressureStatus.Normal
    } else {
        if (
            pressureInBar * barToMillibar > upperPressureWarningThreshold ||
            pressureInBar * barToMillibar < lowerPressureWarningThreshold
        ) {
            pressureStatus = PressureStatus.Critical
        } else {
            pressureStatus = PressureStatus.Normal
        }
    }

    switch (pressureStatus) {
        case PressureStatus.Normal:
            icon_color = tokens.colors.interactive.primary__resting.hex
            break
        case PressureStatus.Critical:
            icon_color = tokens.colors.interactive.warning__resting.hex
            break
        default:
            break
    }

    return (
        <PressureAlignment>
            <Icon name={Icons.Pressure} color={icon_color} size={itemSize} />
            <StyledTopography $fontSize={itemSize}>
                {`${Math.round(pressureInBar * barToMillibar)}mBar`}
            </StyledTopography>
        </PressureAlignment>
    )
}

export default PressureStatusView
