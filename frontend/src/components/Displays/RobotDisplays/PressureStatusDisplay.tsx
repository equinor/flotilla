import { tokens } from '@equinor/eds-tokens'
import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { RobotStatus, RobotType, RobotWithoutTelemetry } from 'models/Robot'
import { useLanguageContext } from 'contexts/LanguageContext'
import { FieldLabel } from 'components/Styles/StyledComponents'

const Field = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`
const PressureAlignment = styled.div`
    display: flex;
    gap: 7px;
    align-items: center;
`
const StyledTypography = styled(Typography)<{ $fontSize?: 24 | 16 | 18 | 32 | 40 | 48 }>`
    font-size: ${(props) => props.$fontSize}px;
`
interface PressureStatusDisplayProps {
    robot: RobotWithoutTelemetry
    pressure?: number
    itemSize?: 24 | 16 | 18 | 32 | 40 | 48 | undefined
}

export const PressureStatusDisplay = ({ robot, pressure, itemSize }: PressureStatusDisplayProps) => {
    const { TranslateText } = useLanguageContext()
    if (robot.type === RobotType.AnymalD || robot.type === RobotType.AnymalX) return null

    const isTaurob = robot.type === RobotType.TaurobInspector || robot.type === RobotType.TaurobOperator
    const isOffline = robot.status === RobotStatus.Offline
    const label = isTaurob ? 'ATEX' : 'Pressure'
    let value: string
    if (isTaurob) {
        value = TranslateText(isOffline ? 'Unknown - robot offline' : 'Active')
    } else {
        if (isOffline || pressure === undefined) return null
        value = `${Math.round(pressure * 1000)}mBar`
    }
    const color =
        isTaurob && !isOffline
            ? tokens.colors.interactive.success__resting.hex
            : tokens.colors.text.static_icons__default.hex
    const icon = isTaurob ? (isOffline ? Icons.CloudOff : Icons.Successful) : Icons.Pressure

    return (
        <Field>
            <FieldLabel>{TranslateText(label)}</FieldLabel>
            <PressureAlignment role="status" aria-label={TranslateText(label)}>
                <Icon name={icon} color={color} size={itemSize} />
                <StyledTypography $fontSize={itemSize} color={color}>
                    {value}
                </StyledTypography>
            </PressureAlignment>
        </Field>
    )
}
