export const formatBackendDateTimeToDate = (date: Date) => {
    return new Date(date.toString())
}

export const getInspectionDeadline = (
    inspectionFrequency: string | undefined,
    lastRunTime: Date | null
): Date | undefined => {
    if (!inspectionFrequency || !lastRunTime) return undefined

    const dayHourSecondsArray = inspectionFrequency.split(':')
    const days: number = +dayHourSecondsArray[0]
    const hours: number = +dayHourSecondsArray[1]
    const minutes: number = +dayHourSecondsArray[2]

    lastRunTime = formatBackendDateTimeToDate(lastRunTime)
    let deadline = lastRunTime
    deadline.setDate(deadline.getDate() + days)
    deadline.setHours(deadline.getHours() + hours)
    deadline.setMinutes(deadline.getMinutes() + minutes)
    return deadline
}

export const getDeadlineInDays = (deadlineDate: Date) => {
    // The magical number on the right is the number of milliseconds in a day
    return new Date(deadlineDate.getTime() - new Date().getTime()).getTime() / 8.64e7
}
