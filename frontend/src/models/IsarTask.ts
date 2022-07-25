import { IsarStep } from './IsarStep'
import { Report } from './Report'

export interface IsarTask {
    Id: string
    IsarTaskId: string
    Report: Report
    TagId: string
    TaskStatus: IsarTaskStatus
    Time: Date
    Steps: IsarStep[]
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
