import { Mission, MissionStatus } from 'models/Mission'
import { TaskStatus } from 'models/Task'

interface MissionStats {
    successCount: number
    failureCount: number
    totalTasksSuccess: number
    totalTasksFailure: number
}

export interface GroupedStats {
    [key: string]: MissionStats
}

export function computeMissionStats(missions: Mission[]): GroupedStats {
    const byRobot: GroupedStats = {}

    for (const mission of missions) {
        const robotKey = mission.robot.name || 'Unknown'

        if (!byRobot[robotKey]) {
            byRobot[robotKey] = {
                successCount: 0,
                failureCount: 0,
                totalTasksSuccess: 0,
                totalTasksFailure: 0,
            }
        }

        if (mission.status === MissionStatus.Successful) {
            byRobot[robotKey].successCount++
        } else if (mission.status === MissionStatus.Failed) {
            byRobot[robotKey].failureCount++
        }

        byRobot[robotKey].totalTasksSuccess += mission.tasks.filter(
            (task) => task.status === TaskStatus.Successful
        ).length
        byRobot[robotKey].totalTasksFailure += mission.tasks.filter((task) => task.status === TaskStatus.Failed).length
    }

    return byRobot
}
