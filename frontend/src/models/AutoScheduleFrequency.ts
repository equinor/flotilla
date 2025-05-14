import { convertUTCDateToLocalDate } from 'utils/StringFormatting'

export interface AutoScheduleFrequency {
    timesOfDayCET: string[] // Format HH:mm:ss
    daysOfWeek: DaysOfWeek[]
    autoScheduledJobs?: string
}

export function parseAutoScheduledJobIds(autoScheduledJobs: string): { [key: string]: string } {
    return JSON.parse(autoScheduledJobs)
}

export enum DaysOfWeek {
    Monday = 'Monday',
    Tuesday = 'Tuesday',
    Wednesday = 'Wednesday',
    Thursday = 'Thursday',
    Friday = 'Friday',
    Saturday = 'Saturday',
    Sunday = 'Sunday',
}

export const allDays = [
    DaysOfWeek.Monday,
    DaysOfWeek.Tuesday,
    DaysOfWeek.Wednesday,
    DaysOfWeek.Thursday,
    DaysOfWeek.Friday,
    DaysOfWeek.Saturday,
    DaysOfWeek.Sunday,
]

export const allDaysIndexOfToday = (convertUTCDateToLocalDate(new Date()).getDay() + 6) % 7
