import { Inspection } from './Inspection'
import { Pose } from './Pose'
import { Position } from './Position'

export interface Task {
    id: string
    isarTaskId?: string
    taskOrder: number
    tagId?: string
    description?: string
    echoTagLink?: string
    inspectionTarget: Position
    robotPose: Pose
    echoPoseId?: number
    status: TaskStatus
    isCompleted: boolean
    startTime?: Date
    endTime?: Date
    error?: string
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
