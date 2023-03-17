import { MissionMap } from './MissionMap'
import { Robot } from './Robot'
import { Task } from './Task'

export enum MissionStatus {
    Pending = 'Pending',
    Ongoing = 'Ongoing',
    Successful = 'Successful',
    PartiallySuccessful = 'PartiallySuccessful',
    Aborted = 'Aborted',
    Failed = 'Failed',
    Paused = 'Paused',
    Cancelled = 'Cancelled',
}

export interface Mission {
    id: string
    echoMissionId: number
    isarMissionId?: string
    name: string
    description?: string
    statusReason?: string
    comment?: string
    assetCode?: string
    robot: Robot
    status: MissionStatus
    isCompleted: boolean
    desiredStartTime: Date
    startTime?: Date
    endTime?: Date
    estimatedDuration?: string
    tasks: Task[]
    map?: MissionMap
}
