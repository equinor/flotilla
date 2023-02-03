import { EchoTag } from './EchoMission'
import { IsarTask } from './IsarTask'
import { MissionMap } from './MissionMap'
import { PlannedTask } from './PlannedTask'
import { Robot } from './Robot'

export enum MissionStatus {
    Pending = 'Pending',
    Ongoing = 'Ongoing',
    Successful = 'Successful',
    Aborted = 'Aborted',
    Failed = 'Failed',
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
    map: MissionMap
    plannedTasks: PlannedTask[]
}
