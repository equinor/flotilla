import { Inspection } from './Inspection'
import { Pose } from './Pose'

export interface Task {
    id: string
    isarTaskId?: string
    taskOrder: number
    tagId?: string
    description?: string
    echoTagLink?: string
    robotPose: Pose
    echoPoseId?: number
    status: TaskStatus
    isCompleted: boolean
    startTime?: Date
    endTime?: Date
    inspections: Inspection[]
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
