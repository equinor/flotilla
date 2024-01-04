import { Robot } from 'models/Robot'

export const filterRobots = (robots: Robot[], id: string): Robot[] => {
    const desiredRobot = robots.filter((robot: Robot) => robot.id === id)
    return desiredRobot
}
