import { defaultRobots, Robot } from './robot'

export enum MissionStatus {
    Pending = 'Pending',
    Started = 'Started',
    Successful = 'Successful',
    Failed = 'Failed',
}

export class Mission {
    id: string
    robot: Robot
    isarMissionId: string
    startTime: Date
    endTime: Date
    status: MissionStatus

    constructor(
        id: string,
        robot: Robot,
        isarMissionId: string,
        startTime: Date,
        endTime: Date,
        status: MissionStatus
    ) {
        this.id = id
        this.robot = robot
        this.isarMissionId = isarMissionId
        this.startTime = startTime
        this.endTime = endTime
        this.status = status
    }
}

export const defaultMission: { [name: string]: Mission } = {
    test1: new Mission('1', defaultRobots['Taurob'], '2', new Date(), new Date(500000000), MissionStatus.Pending),
}
