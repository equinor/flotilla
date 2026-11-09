import { RobotStatus, RobotWithoutTelemetry } from 'models/Robot'

export type AtexPressureStatus = 'offline' | 'unknown' | 'unconfigured' | 'low' | 'high' | 'normal'

export const getAtexPressureStatus = (
    robot: RobotWithoutTelemetry,
    pressure: number | undefined
): AtexPressureStatus => {
    if (robot.status === RobotStatus.Offline) return 'offline'
    if (robot.status === RobotStatus.ConnectionIssues || robot.status === RobotStatus.UnknownStatus) return 'unknown'

    const lower = robot.lowerPressureWarningThreshold
    const upper = robot.upperPressureWarningThreshold
    if (lower == null || upper == null || !Number.isFinite(lower) || !Number.isFinite(upper) || lower >= upper)
        return 'unconfigured'

    if (pressure === undefined || !Number.isFinite(pressure)) return 'unknown'
    if (pressure < lower) return 'low'
    if (pressure > upper) return 'high'
    return 'normal'
}
