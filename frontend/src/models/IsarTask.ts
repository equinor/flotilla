import { IsarStep } from './IsarStep'
import { Mission } from './Mission'

export interface IsarTask {
    id: string
    isarTaskId: string
    mission: Mission
    tagId?: string
    taskStatus: IsarTaskStatus
    time: Date
    steps: IsarStep[]
}

export enum IsarTaskStatus {
    Successful = 'Successful',
    PartiallySuccessful = 'PartiallySuccessful',
    NotStarted = 'NotStarted',
    InProgress = 'InProgress',
    Failed = 'Failed',
    Cancelled = 'Cancelled',
    Paused = 'Paused',
}

export namespace IsarTaskStatus {
    export function isComplete(status: IsarTaskStatus): boolean {
        if (
            status === IsarTaskStatus.Successful ||
            status === IsarTaskStatus.PartiallySuccessful ||
            status === IsarTaskStatus.Failed ||
            status === IsarTaskStatus.Cancelled
        ) {
            return true
        }
        return false
    }
}
