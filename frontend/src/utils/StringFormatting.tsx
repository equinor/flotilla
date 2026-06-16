import { format } from 'date-fns'

export const convertUTCDateToLocalDate = (date: Date | string): Date => {
    // If the input is actually a string (e.g. from JSON), ensure it is interpreted as UTC
    if (typeof date === 'string') {
        const dateStr = date as string
        return new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z')
    }
    // If it is already a proper Date object, it already represents the correct point in time
    return date
}

export const formatDateTime = (dateTime: Date | string, dateFormat: string = 'dd/MM/yy - HH:mm'): string =>
    format(convertUTCDateToLocalDate(dateTime), dateFormat)

export const capitalizeFirstLetter = (str: string) => {
    return str.charAt(0).toUpperCase() + str.slice(1)
}
