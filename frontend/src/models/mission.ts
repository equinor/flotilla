import { defaultRobots, Robot } from './robot'

export enum MissionStatus {
    Pending = 'Pending',
    Started = 'Started',
    Successful = 'Successful',
    Failed = 'Failed',
    Warning = 'Warning',
}

export class Mission {
    id: string
    name: string
    robot: Robot
    isarMissionId: string
    startTime: Date
    endTime: Date
    status: MissionStatus

    constructor(
        id: string,
        name: string,
        robot: Robot,
        isarMissionId: string,
        startTime: Date,
        endTime: Date,
        status: MissionStatus
    ) {
        this.id = id
        this.name = name
        this.robot = robot
        this.isarMissionId = isarMissionId
        this.startTime = startTime
        this.endTime = endTime
        this.status = status
    }
}

export const defaultMission: { [name: string]: Mission } = {
    Pending: new Mission(
        '1',
        'Test Mission Pending',
        defaultRobots['Taurob'],
        '2',
        new Date(),
        new Date(500000000),
        MissionStatus.Pending
    ),
    Warning: new Mission(
        '1',
        'Test Mission Warning',
        defaultRobots['Taurob'],
        '2',
        new Date(),
        new Date(500000000),
        MissionStatus.Warning
    ),
    Started: new Mission(
        '1',
        'Test Mission Started',
        defaultRobots['Taurob'],
        '2',
        new Date(),
        new Date(500000000),
        MissionStatus.Started
    ),
    Failed: new Mission(
        '1',
        'Test Mission Failed',
        defaultRobots['Taurob'],
        '2',
        new Date(),
        new Date(500000000),
        MissionStatus.Failed
    ),
    Successful: new Mission(
        '1',
        'Test Mission Successful',
        defaultRobots['Taurob'],
        '2',
        new Date(),
        new Date(500000000),
        MissionStatus.Successful
    ),
}
