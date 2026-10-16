import { describe, expect, test } from 'vitest'
import {
    AutoScheduleOccurrence,
    canSkipOccurrence,
    findOccurrenceForSlot,
    formatScheduledAt,
    upcomingScheduledOccurrences,
} from 'models/AutoScheduleOccurrence'
import { DaysOfWeek } from 'models/AutoScheduleFrequency'

const makeOccurrence = (
    id: string,
    scheduledAt: string,
    status: 'Scheduled' | 'Skipped' = 'Scheduled'
): AutoScheduleOccurrence => ({
    id,
    scheduledAt,
    status,
    mission: {
        id: 'mission',
        name: 'Inspection',
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

    test('includes tomorrow, excludes past and skipped occurrences, and sorts by instant', () => {
        const today = makeOccurrence('today', '2026-10-16T21:00:00Z')
        const tomorrow = makeOccurrence('tomorrow', '2026-10-16T23:00:00Z')
        expect(
            upcomingScheduledOccurrences(
                [
                    tomorrow,
                    makeOccurrence('past', '2026-10-16T19:00:00Z'),
                    makeOccurrence('skip', '2026-10-17T02:00:00Z', 'Skipped'),
                    today,
                    makeOccurrence('outside', '2026-10-17T20:00:01Z'),
                ],
                now
            )
        ).toEqual([today, tomorrow])
    })

    test('uses an exclusive lower and inclusive upper boundary', () => {
        expect(canSkipOccurrence(makeOccurrence('now', now.toISOString()), now)).toBe(false)
        expect(canSkipOccurrence(makeOccurrence('boundary', '2026-10-17T20:00:00Z'), now)).toBe(true)
    })

    test('formats the actual Oslo date independently of browser timezone', () => {
        const text = formatScheduledAt('2026-10-16T23:00:00Z', 'en')
        expect(text).toContain('17/10')
        expect(text).toContain('01:00')
    })

    test('does not confuse equal times on different days', () => {
        const today = makeOccurrence('today', '2026-10-16T08:00:00Z')
        const tomorrow = makeOccurrence('tomorrow', '2026-10-17T08:00:00Z')
        expect(findOccurrenceForSlot([today, tomorrow], 'mission', DaysOfWeek.Saturday, '10:00:00')?.id).toBe(
            'tomorrow'
        )
    })

    test('uses elapsed hours across the autumn clock change', () => {
        const beforeFallback = new Date('2026-10-24T12:00:00Z')
        expect(canSkipOccurrence(makeOccurrence('inside', '2026-10-25T12:00:00Z'), beforeFallback)).toBe(true)
        expect(canSkipOccurrence(makeOccurrence('outside', '2026-10-25T12:30:00Z'), beforeFallback)).toBe(false)
    })
})
