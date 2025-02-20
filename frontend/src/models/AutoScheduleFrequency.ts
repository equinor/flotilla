export interface AutoScheduleFrequency {
    timesOfDay: string[] // Format HH:mm:ss
    daysOfWeek: DaysOfWeek[]
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
