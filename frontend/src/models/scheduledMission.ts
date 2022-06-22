import { Robot } from 'models/robot'

export class ScheduledMission {
    id: string
    robot: Robot
    isarMissionId: string
    status: ScheduledMissionStatus

    constructor(id: string, robot: Robot, isarMissionId: string, status: ScheduledMissionStatus) {
        this.id = id
        this.robot = robot
        this.isarMissionId = isarMissionId
        this.status = status
    }
}
export enum ScheduledMissionStatus {
    Pending = 'Pending',
    Started = 'Started',
    Successful = 'Successful',
    Failed = 'Failed',
}
