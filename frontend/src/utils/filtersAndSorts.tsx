import { RobotWithoutTelemetry } from 'models/Robot'

export const filterRobots = (robots: RobotWithoutTelemetry[], id: string): RobotWithoutTelemetry[] => {
    const desiredRobot = robots.filter((robot: RobotWithoutTelemetry) => robot.id === id)
    return desiredRobot
}
