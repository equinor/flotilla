import { MissionDefinition } from './MissionDefinition'
import { allDays, DaysOfWeek, getAllDaysIndexOfToday, getCetTimeOnly } from './AutoScheduleFrequency'

export enum AutoScheduleOccurrenceStatus {
    Scheduled = 'Scheduled',
    Skipped = 'Skipped',
}

export interface AutoScheduleOccurrence {
    scheduledAt: string
    status: AutoScheduleOccurrenceStatus
    skipId: string | null
    mission: MissionDefinition
}

export const canChangeOccurrenceSkip = (occurrence: AutoScheduleOccurrence, now: Date): boolean => {
    const scheduledAt = new Date(occurrence.scheduledAt).getTime()
    return scheduledAt > now.getTime() && scheduledAt <= now.getTime() + 24 * 60 * 60 * 1000
}

// Existing fractional schedules retain the database's microsecond precision.
const postgresFraction = (time: string): string =>
    time
        .match(/\.(\d+)/)?.[1]
        .slice(0, 6)
        .replace(/0+$/, '') ?? ''

export const findOccurrenceForSlot = (
    occurrences: AutoScheduleOccurrence[],
    missionId: string,
    day: DaysOfWeek,
    time: string
) =>
    occurrences.find((occurrence) => {
        const instant = new Date(occurrence.scheduledAt)
        return (
            occurrence.mission.id === missionId &&
            allDays[getAllDaysIndexOfToday(instant)] === day &&
            getCetTimeOnly(instant) === time.split('.')[0] &&
            postgresFraction(occurrence.scheduledAt) === postgresFraction(time)
        )
    })

export const upcomingOccurrences = (occurrences: AutoScheduleOccurrence[], now: Date) =>
    occurrences
        .filter((occurrence) => canChangeOccurrenceSkip(occurrence, now))
        .sort((a, b) => Date.parse(a.scheduledAt) - Date.parse(b.scheduledAt))

export const upcomingScheduledOccurrences = (occurrences: AutoScheduleOccurrence[], now: Date) =>
    upcomingOccurrences(occurrences, now).filter(
        (occurrence) => occurrence.status === AutoScheduleOccurrenceStatus.Scheduled
    )

export const formatScheduledAt = (scheduledAt: string, language: string): string =>
    new Intl.DateTimeFormat(language === 'no' ? 'nb-NO' : 'en-GB', {
        timeZone: 'Europe/Oslo',
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23',
    }).format(new Date(scheduledAt))
