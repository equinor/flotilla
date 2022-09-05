import { Robot } from 'models/Robot'

export const filterRobots = (robots: Robot[], name: string): Robot[] => {
    const desiredRobot = robots.filter((robot: Robot) => robot.name === name)
    return desiredRobot
}
