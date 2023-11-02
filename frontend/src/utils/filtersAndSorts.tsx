import { compareDesc } from 'date-fns'
import { Robot } from 'models/Robot'

export const filterRobots = (robots: Robot[], id: string): Robot[] => {
    const desiredRobot = robots.filter((robot: Robot) => robot.id === id)
    return desiredRobot
}

export const compareByDate = (timeA?: Date, timeB?: Date): number => {
    return compareDesc(!timeA ? new Date(0) : new Date(timeA), !timeB ? new Date(0) : new Date(timeB))
}
