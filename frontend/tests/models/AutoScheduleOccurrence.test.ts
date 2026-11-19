import { describe, expect, test } from 'vitest'
import {
    AutoScheduleOccurrence,
    AutoScheduleOccurrenceStatus,
    canChangeOccurrenceSkip,
    findOccurrenceForSlot,
    formatScheduledAt,
    upcomingScheduledOccurrences,
    upcomingOccurrences,
} from 'models/AutoScheduleOccurrence'
import { DaysOfWeek } from 'models/AutoScheduleFrequency'

const makeOccurrence = (
    name: string,
    scheduledAt: string,
    status: AutoScheduleOccurrenceStatus = AutoScheduleOccurrenceStatus.Scheduled
): AutoScheduleOccurrence => ({
    scheduledAt,
    status,
    skipId: status === AutoScheduleOccurrenceStatus.Skipped ? '42' : null,
    mission: {
        id: 'mission',
        name,
        isAdHoc: false,
        installationCode: 'test',
        inspectionArea: {
            id: 'area',
            inspectionAreaName: 'Area',
            plantName: 'Plant',
            plantCode: 'plant',
            installationCode: 'test',
        },
        tasks: [],
    },
})

describe('24-hour occurrences', () => {
    const now = new Date('2026-10-16T20:00:00Z')

    test('lists upcoming missions in dated order, excluding skips only on the dashboard', () => {
        const planned = makeOccurrence('planned', '2026-10-17T01:00:00Z')
        const skipped = makeOccurrence('skipped', '2026-10-16T23:00:00Z', AutoScheduleOccurrenceStatus.Skipped)
        const pastSkip = makeOccurrence('past', now.toISOString(), AutoScheduleOccurrenceStatus.Skipped)
        const outsideSkip = makeOccurrence('outside', '2026-10-17T20:00:01Z', AutoScheduleOccurrenceStatus.Skipped)
        const entries = [planned, outsideSkip, skipped, pastSkip]
        expect(upcomingOccurrences(entries, now)).toEqual([skipped, planned])
        expect(upcomingScheduledOccurrences(entries, now)).toEqual([planned])
        expect(formatScheduledAt(skipped.scheduledAt, 'en')).toMatch(/17\/10.*01:00/)
    })

    test('does not confuse equal times on different days', () => {
        const today = makeOccurrence('today', '2026-10-16T08:00:00Z')
        const tomorrow = makeOccurrence('tomorrow', '2026-10-17T08:00:00Z')
        expect(findOccurrenceForSlot([today, tomorrow], 'mission', DaysOfWeek.Saturday, '10:00:00')).toBe(tomorrow)
    })

    test('skip and undo use the same exclusive/inclusive 24 elapsed-hour window across DST', () => {
        const beforeFallback = new Date('2026-10-24T12:00:00Z')
        for (const status of [AutoScheduleOccurrenceStatus.Scheduled, AutoScheduleOccurrenceStatus.Skipped]) {
            expect(
                canChangeOccurrenceSkip(makeOccurrence('now', beforeFallback.toISOString(), status), beforeFallback)
            ).toBe(false)
            expect(
                canChangeOccurrenceSkip(makeOccurrence('inside', '2026-10-25T12:00:00Z', status), beforeFallback)
            ).toBe(true)
            expect(
                canChangeOccurrenceSkip(makeOccurrence('outside', '2026-10-25T12:00:01Z', status), beforeFallback)
            ).toBe(false)
        }
    })

    test('matches legacy fractional slots without confusing adjacent instants', () => {
        const first = makeOccurrence('first', '2026-10-17T01:00:00.123456+02:00')
        const second = makeOccurrence('second', '2026-10-16T23:00:00.123457Z')
        const entries = [first, second]
        expect(findOccurrenceForSlot(entries, 'mission', DaysOfWeek.Saturday, '01:00:00.1234567')).toBe(first)
        expect(findOccurrenceForSlot(entries, 'mission', DaysOfWeek.Saturday, '01:00:00.123457')).toBe(second)
        expect(findOccurrenceForSlot(entries, 'mission', DaysOfWeek.Saturday, '01:00:00')).toBeUndefined()
    })
})
