import { Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { RobotWithoutTelemetry } from 'models/Robot'
import { AtexPressureStatus, getAtexPressureStatus } from 'utils/atexPressure'
import { Icons } from 'utils/icons'

const Status = styled.div`
    display: flex;
    flex-direction: column;
    gap: 4px;
`

const Badge = styled.div<{ $color: string }>`
    display: flex;
    align-items: center;
    gap: 6px;
    color: ${(props) => props.$color};
`

const labels: Record<AtexPressureStatus, string> = {
    normal: 'Within limits',
    low: 'Pressure too low',
    high: 'Pressure too high',
    offline: 'Unknown - robot offline',
    unknown: 'Unknown - no fresh pressure',
    unconfigured: 'Limits not configured',
}

export const AtexStatusDisplay = ({ robot, pressure }: { robot: RobotWithoutTelemetry; pressure?: number }) => {
    const { TranslateText } = useLanguageContext()
    if (!robot.isAtex) return null

    const status = getAtexPressureStatus(robot, pressure)
    const isAlarm = status === 'low' || status === 'high'
    const hasReading = status === 'normal' || isAlarm
    const color = isAlarm
        ? tokens.colors.interactive.danger__resting.hex
        : status === 'normal'
          ? tokens.colors.interactive.success__resting.hex
          : tokens.colors.text.static_icons__secondary.hex
    const range =
        robot.lowerPressureWarningThreshold != null && robot.upperPressureWarningThreshold != null
            ? TranslateText('Pressure limits: {0} to {1} mbar', [
                  String(robot.lowerPressureWarningThreshold * 1000),
                  String(robot.upperPressureWarningThreshold * 1000),
              ])
            : TranslateText('Limits not configured')

    return (
        <Tooltip title={`${range}. ${TranslateText('Pressure indication only, not a safety guarantee')}`}>
            <Status role="status" aria-label={TranslateText('ATEX pressure')} tabIndex={0}>
                <Badge $color={color}>
                    <Icon name={Icons.Pressure} size={24} color={color} />
                    <Typography color={color}>{TranslateText(labels[status])}</Typography>
                </Badge>
                {hasReading && pressure !== undefined && (
                    <Typography variant="caption">{`${Math.round(pressure * 1000)} mbar`}</Typography>
                )}
            </Status>
        </Tooltip>
    )
}
