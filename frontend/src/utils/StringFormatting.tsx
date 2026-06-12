import { format } from 'date-fns'

export const convertUTCDateToLocalDate = (date: Date): Date => {
    // If lastChar is Z, typescript assumes the date to be UTC
    const lastChar = date.toString().slice(-1)
    if (lastChar === 'Z') return new Date(date)
    // If not, typescript assumes the date to be local time
    return new Date(new Date(date).getTime() - new Date(date).getTimezoneOffset() * 60 * 1000)
}

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
