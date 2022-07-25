import { IsarTask } from './IsarTask'
import { Robot } from './Robot'

export interface Report {
    Id: string
    Robot: Robot
    IsarMissionId: string
    EchoMissionId: number
    Log: string
    ReportStatus: ReportStatus
    StartTime: Date
    EndTime: Date
    Tasks: IsarTask[]
}

enum ReportStatus {
    Successful,
    NotStarted,
    InProgress,
    Failed,
    Cancelled,
    Paused,
}
