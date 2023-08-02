import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { PressureStatus } from 'models/Pressure'
import { RobotStatus } from 'models/Robot'

const PressureAlignment = styled.div`
    display: flex;
    align-items: center;
`

const StyledTopography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize};
`
export interface PressureStatusViewProps {
    pressureInBar?: number
    pressureInMilliBar?: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
    upperPressureWarningThreshold?: number
    lowerPressureWarningThreshold?: number
    robotStatus?: RobotStatus
}

const PressureStatusView = ({
    robotStatus,
    pressureInBar,
    itemSize,
    upperPressureWarningThreshold,
    lowerPressureWarningThreshold,
}: PressureStatusViewProps): JSX.Element => {
    const barToMillibar = 1000
    let icon_color: string = tokens.colors.interactive.primary__resting.hex
    let pressureStatus: PressureStatus
    let pressureInMilliBar: string = ''

    if (!pressureInBar) {
        pressureInMilliBar = ''
        pressureStatus = PressureStatus.Default
        return <></>
    } else if (robotStatus == RobotStatus.Offline) {
        pressureInMilliBar = ''
        pressureStatus = PressureStatus.Default
    } else if (!upperPressureWarningThreshold || !lowerPressureWarningThreshold) {
        pressureStatus = PressureStatus.Normal
    } else {
        if (
            pressureInBar * barToMillibar > upperPressureWarningThreshold ||
            pressureInBar * barToMillibar < lowerPressureWarningThreshold
        ) {
            pressureStatus = PressureStatus.Critical
            pressureInMilliBar = `${Math.round(pressureInBar * barToMillibar)}mBar`
        } else {
            pressureStatus = PressureStatus.Normal
            pressureInMilliBar = `${Math.round(pressureInBar * barToMillibar)}mBar`
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
            icon_color = tokens.colors.interactive.disabled__text.hex
            break
    }

    return (
        <PressureAlignment>
            <Icon name={Icons.Pressure} color={icon_color} size={itemSize} />
            <StyledTopography $fontSize={itemSize}>{pressureInMilliBar}</StyledTopography>
        </PressureAlignment>
    )
}

export default PressureStatusView
