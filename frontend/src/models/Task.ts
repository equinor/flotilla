import { Inspection } from './Inspection'
import { AnalysisType } from './MissionDefinition'
import { Pose } from './Pose'

export interface Task {
    id: string
    taskOrder: number
    tagId?: string
    description?: string
    robotPose: Pose
    analysisTypes: AnalysisType[]
    status: TaskStatus
    isCompleted: boolean
    startTime?: Date
    endTime?: Date
    inspection: Inspection
    errorDescription?: string
}

export enum TaskStatus {
    Successful = 'Successful',
    PartiallySuccessful = 'PartiallySuccessful',
    NotStarted = 'NotStarted',
    InProgress = 'InProgress',
    Failed = 'Failed',
    Cancelled = 'Cancelled',
    Paused = 'Paused',
}
