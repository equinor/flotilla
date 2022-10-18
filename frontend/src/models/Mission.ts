import { EchoTag } from './EchoMission'
import { IsarTask } from './IsarTask'
import { Robot } from './Robot'

export enum MissionStatus {
    Pending = 'Pending',
    Ongoing = 'Ongoing',
    Successful = 'Successful',
    Aborted = 'Aborted',
    Warning = 'Warning',
    Paused = 'Paused',
    Cancelled = 'Cancelled',
}

export interface Mission {
    id: string
    name: string
    assetCode?: string
    robot: Robot
    isarMissionId?: string
    echoMissionId: number
    missionStatus: MissionStatus
    startTime: Date
    endTime?: Date
    tasks: IsarTask[]
    plannedTasks: EchoTag[]
}
