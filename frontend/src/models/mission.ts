import { Robot } from './robot'

export class Mission {
    id: string
    robot: Robot
    isarMissionId: string
    startTime: Date
    endTime: Date

    constructor(id: string, robot: Robot, isarMissionId: string, startTime: Date, endTime: Date) {
        this.id = id
        this.robot = robot
        this.isarMissionId = isarMissionId
        this.startTime = startTime
        this.endTime = endTime
    }
}
