import { MissionDefinition } from './MissionDefinition'
import { allDays, DaysOfWeek, getAllDaysIndexOfToday, getCetTimeOnly } from './AutoScheduleFrequency'

export interface ScheduledMissionRun {
    id: string
    scheduledAt: string
    status: 'Scheduled' | 'Skipped'
    mission: MissionDefinition
}

const isInUpcomingWindow = (occurrence: ScheduledMissionRun, now: Date): boolean => {
    const scheduledAt = new Date(occurrence.scheduledAt).getTime()
    return scheduledAt > now.getTime() && scheduledAt <= now.getTime() + 24 * 60 * 60 * 1000
}

export const canSkipScheduledRun = (occurrence: ScheduledMissionRun, now: Date): boolean =>
    occurrence.status === 'Scheduled' && isInUpcomingWindow(occurrence, now)

export const findRunForSlot = (occurrences: ScheduledMissionRun[], missionId: string, day: DaysOfWeek, time: string) =>
    occurrences.find((occurrence) => {
        const instant = new Date(occurrence.scheduledAt)
        return (
            occurrence.mission.id === missionId &&
            allDays[getAllDaysIndexOfToday(instant)] === day &&
            getCetTimeOnly(instant) === time
        )
    })

export const upcomingScheduledRuns = (occurrences: ScheduledMissionRun[], now: Date) =>
    occurrences
        .filter((occurrence) => canSkipScheduledRun(occurrence, now))
        .sort((a, b) => Date.parse(a.scheduledAt) - Date.parse(b.scheduledAt) || a.id.localeCompare(b.id))

export const formatScheduledAt = (scheduledAt: string, language: string): string =>
    new Intl.DateTimeFormat(language === 'no' ? 'nb-NO' : 'en-GB', {
        timeZone: 'Europe/Oslo',
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23',
    }).format(new Date(scheduledAt))
