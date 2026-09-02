import type { MissionDefinition } from './MissionDefinition'

export interface AutoScheduleFrequency {
    schedulingTimesCETperWeek: TimeAndDay[]
    autoScheduledJobs?: string
}

export interface TimeAndDay {
    dayOfWeek: DaysOfWeek
    timeOfDay: string // Format HH:mm:ss
}

function parseAutoScheduledJobIds(autoScheduledJobs: string): { [key: string]: string } {
    return JSON.parse(autoScheduledJobs)
}

export const isJobScheduledAt = (mission: MissionDefinition, time: string): boolean =>
    !!mission.autoScheduleFrequency?.autoScheduledJobs &&
    !!parseAutoScheduledJobIds(mission.autoScheduleFrequency.autoScheduledJobs)[time]

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

export const allDaysStartingSunday = [
    DaysOfWeek.Sunday,
    DaysOfWeek.Monday,
    DaysOfWeek.Tuesday,
    DaysOfWeek.Wednesday,
    DaysOfWeek.Thursday,
    DaysOfWeek.Friday,
    DaysOfWeek.Saturday,
]

// SchedulingTimesCETperWeek is always CET/CEST, regardless of server or browser timezone
const getCetNow = (now: Date): Date => {
    const parts = new Intl.DateTimeFormat('en-US', {
        timeZone: 'Europe/Oslo',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: false,
    }).formatToParts(now)
    const get = (type: string) => Number(parts.find((p) => p.type === type)!.value)
    return new Date(get('year'), get('month') - 1, get('day'), get('hour') % 24, get('minute'), get('second'))
}

export const getCetTimeOnly = (now: Date): string => {
    const cetNow = getCetNow(now)
    const pad = (n: number) => n.toString().padStart(2, '0')
    return `${pad(cetNow.getHours())}:${pad(cetNow.getMinutes())}:${pad(cetNow.getSeconds())}`
}

export const getAllDaysIndexOfToday = (now: Date) => (getCetNow(now).getDay() + 6) % 7

interface AutoScheduledOccurrence {
    mission: MissionDefinition
    dayOfWeek: DaysOfWeek
    timeOfDay: string
}

export const getAutoScheduledOccurrences = (missionDefinitions: MissionDefinition[]): AutoScheduledOccurrence[] =>
    missionDefinitions
        .filter((m) => m.autoScheduleFrequency)
        .flatMap((mission) =>
            mission.autoScheduleFrequency!.schedulingTimesCETperWeek.map((timeAndDay) => ({
                mission,
                dayOfWeek: timeAndDay.dayOfWeek,
                timeOfDay: timeAndDay.timeOfDay,
            }))
        )

export const getTimeMissionPairsForDay = (missionDefinitions: MissionDefinition[], day: DaysOfWeek) =>
    getAutoScheduledOccurrences(missionDefinitions)
        .filter((occurrence) => occurrence.dayOfWeek === day)
        .map(({ timeOfDay, mission }) => ({ time: timeOfDay, mission }))
        .sort((a, b) => (a.time === b.time ? 0 : a.time > b.time ? 1 : -1))
