import { InspectionArea } from './InspectionArea'
import { Robot, placeholderRobot } from './Robot'
import { Task } from './Task'

export enum MissionStatus {
    Pending = 'Pending',
    Ongoing = 'Ongoing',
    Paused = 'Paused',
    Aborted = 'Aborted',
    Cancelled = 'Cancelled',
    Failed = 'Failed',
    Successful = 'Successful',
    PartiallySuccessful = 'PartiallySuccessful',
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
    missionId: string
    name: string
    description?: string
    statusReason?: string
    comment?: string
    installationCode: string
    inspectionArea: InspectionArea
    robot: Robot
    status: MissionStatus
    isCompleted: boolean
    creationTime: Date
    startTime?: Date
    endTime?: Date
    estimatedTaskDuration?: number
    tasks: Task[]
}

export const placeholderMission: Mission = {
    id: 'placeholderId',
    name: 'placeholderMission',
    missionId: 'placeholderMissionId',
    robot: placeholderRobot,
    installationCode: 'placeholderInstallationCode',
    inspectionArea: {
        id: 'placeholderInspectionAreaId',
        inspectionAreaName: 'placeholderInspectionArea',
        plantName: 'placeholderPlantName',
        installationCode: 'placeholderInstallationCode',
    },
    status: MissionStatus.Pending,
    isCompleted: false,
    creationTime: new Date(),
    tasks: [],
}
