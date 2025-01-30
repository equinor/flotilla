import { differenceInSeconds } from 'date-fns'
import { Task, TaskStatus } from 'models/Task'
import { convertUTCDateToLocalDate } from './StringFormatting'

export const calculateRemaindingTimeInMinutes = (tasks: Task[], estimatedTaskDuration: number) => {
    const estimatedTaskDurations = tasks.map((task) => {
        if (task.status === TaskStatus.NotStarted || task.status === TaskStatus.Paused) {
            return estimatedTaskDuration + (task.inspection.videoDuration ?? 0)
        } else if (task.status === TaskStatus.InProgress) {
            const timeUsed = task.startTime
                ? differenceInSeconds(Date.now(), convertUTCDateToLocalDate(task.startTime))
                : 0
            return Math.max(estimatedTaskDuration + (task.inspection.videoDuration ?? 0) - timeUsed, 0)
        }
        return 0
    })
    const remandingTimeInSeconds = estimatedTaskDurations.reduce((sum, x) => sum + x)
    return Math.ceil(remandingTimeInSeconds / 60)
}
