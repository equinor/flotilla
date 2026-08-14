interface MissionStatistics {
    total: number
    successful: number
    partiallySuccessful: number
    failed: number
    aborted: number
    cancelled: number
    successRate: number
}

interface TaskStatistics {
    total: number
    successful: number
    partiallySuccessful: number
    successRate: number
}

interface WeeklyMissionCount {
    weekStart: string
    weekEnd: string
    count: number
}

export interface RobotStatistics {
    robotId: string
    fromTime: string
    toTime: string
    missions: MissionStatistics
    tasks: TaskStatistics
    missionsPerWeek: WeeklyMissionCount[]
}
