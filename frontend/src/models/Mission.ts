import { Robot } from './Robot'

export enum MissionStatus {
    Pending = 'Pending',
    Ongoing = 'Ongoing',
    Successful = 'Successful',
    Aborted = 'Aborted',
    Warning = 'Warning',
    Paused = 'Paused',
}

export interface Mission {
    id: string
    name: string
    robot: Robot
    echoMissionId: string
    startTime: Date
    endTime: Date
    status: MissionStatus
}
