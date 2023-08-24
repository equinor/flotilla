export const formatBackendDateTimeToDate = (date: Date) => {
    return new Date(date.toString())
}

export const getInspectionDeadline = (inspectionFrequency: string, lastRunTime: Date): Date => {
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
    // More flexibly we can also define the deadline in terms of milliseconds:
    // new Date(lastRunTime.getTime() + (1000 * 60 * days) + (1000 * 60 * 60 * hours) + (1000 * 60 * 60 * 24 * days))
}
