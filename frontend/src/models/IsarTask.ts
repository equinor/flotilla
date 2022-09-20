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

enum IsarTaskStatus {
    Successful,
    PartiallySuccessful,
    NotStarted,
    InProgress,
    Failed,
    Cancelled,
    Paused,
}
