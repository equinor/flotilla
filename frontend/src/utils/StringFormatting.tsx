import { format } from 'date-fns'

const millisecondsInADay = 8.64e7

export const convertUTCDateToLocalDate = (date: Date): Date =>
    new Date(date.getTime() - date.getTimezoneOffset() * 60 * 1000)

const formatBackendDateTimeToDate = (date: Date) => new Date(date.toString())

export const getInspectionDeadline = (
    inspectionFrequency: string | undefined,
    lastSuccessfulRunTime: Date | null
): Date | undefined => {
    if (!inspectionFrequency || !lastSuccessfulRunTime) return undefined

    const dayHourSecondsArray = inspectionFrequency.split(':')
    const days: number = +dayHourSecondsArray[0]
    const hours: number = +dayHourSecondsArray[1]
    const minutes: number = +dayHourSecondsArray[2]

    lastSuccessfulRunTime = formatBackendDateTimeToDate(lastSuccessfulRunTime)
    let deadline = lastSuccessfulRunTime
    deadline.setDate(deadline.getDate() + days)
    deadline.setHours(deadline.getHours() + hours)
    deadline.setMinutes(deadline.getMinutes() + minutes)
    return deadline
}

export const getDeadlineInDays = (deadlineDate: Date): number =>
    new Date(deadlineDate.getTime() - new Date().getTime()).getTime() / millisecondsInADay

export const formatDateTime = (dateTime: Date, dateFormat: string): string =>
    format(convertUTCDateToLocalDate(new Date(dateTime)), dateFormat)
