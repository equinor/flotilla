import { format } from 'date-fns'

const millisecondsInADay = 8.64e7

export const convertUTCDateToLocalDate = (date: Date): Date => {
    // If lastChar is Z, typescript assumes the date to be UTC
    const lastChar = date.toString().slice(-1)
    if (lastChar === 'Z') return new Date(date)
    // If not, typescript assumes the date to be local time
    return new Date(new Date(date).getTime() - new Date(date).getTimezoneOffset() * 60 * 1000)
}

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
    const deadline = lastSuccessfulRunTime
    deadline.setDate(deadline.getDate() + days)
    deadline.setHours(deadline.getHours() + hours)
    deadline.setMinutes(deadline.getMinutes() + minutes)
    return deadline
}

export const getDeadlineInDays = (deadlineDate: Date): number =>
    new Date(deadlineDate.getTime() - new Date().getTime()).getTime() / millisecondsInADay

export const formatDateTime = (dateTime: Date, dateFormat: string): string =>
    format(convertUTCDateToLocalDate(dateTime), dateFormat)

export const capitalizeFirstLetter = (str: string) => {
    return str.charAt(0).toUpperCase() + str.slice(1)
}

export const formatDateString = (dateStr: Date | string) => {
    let newStr = dateStr.toString()
    newStr = newStr.slice(0, 19)
    newStr = newStr.replaceAll('T', ' ')
    return newStr
}
