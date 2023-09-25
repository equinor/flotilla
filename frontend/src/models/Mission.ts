import { MapMetadata } from './MapMetadata'
import { Robot, dummyRobot } from './Robot'
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

export const missionStatusFilterOptionsIterable = [
    MissionStatus.Successful,
    MissionStatus.PartiallySuccessful,
    MissionStatus.Aborted,
    MissionStatus.Failed,
    MissionStatus.Cancelled,
] as const
export type MissionStatusFilterOptions = (typeof missionStatusFilterOptionsIterable)[number]

export interface Mission {
    id: string
    echoMissionId?: number
    isarMissionId?: string
    name: string
    description?: string
    statusReason?: string
    comment?: string
    installationCode?: string
    robot: Robot
    status: MissionStatus
    isCompleted: boolean
    desiredStartTime: Date
    startTime?: Date
    endTime?: Date
    estimatedDuration?: number
    tasks: Task[]
    map?: MapMetadata
}
export const dummyMission: Mission = {
    id: 'dummyId',
    name: 'dummyMission',
    robot: dummyRobot,
    status: MissionStatus.Pending,
    isCompleted: false,
    desiredStartTime: new Date(),
    tasks: [],
}
