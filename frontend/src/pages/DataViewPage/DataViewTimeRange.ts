export const timeRangePresets = [
    { days: 7, label: '7 days' },
    { days: 30, label: '1 month' },
    { days: 90, label: '3 months' },
] as const

export type PresetDays = (typeof timeRangePresets)[number]['days']

export type TimeRangeMode = PresetDays | 'custom'

export interface DataViewTimeRange {
    minDate: Date
    maxDate: Date | null
}

type CustomDateRangeField = 'start' | 'end'
type CustomDateRangeError = 'missing-date' | 'invalid-range' | 'future-date'

export interface CustomDateRangeFailure {
    error: CustomDateRangeError
    invalidFields: CustomDateRangeField[]
}

type CustomDateRangeResult =
    { range: DataViewTimeRange; failure?: never } | { range?: never; failure: CustomDateRangeFailure }

// Snapped to midnight so that reselecting a preset reuses the cached query instead ofs
// producing a new key on every click.
export const createPresetTimeRange = (days: PresetDays, currentTime: Date = new Date()): DataViewTimeRange => {
    const minDate = new Date(currentTime)
    minDate.setDate(minDate.getDate() - days)
    minDate.setHours(0, 0, 0, 0)

    return { minDate, maxDate: null }
}

const parseLocalDate = (value: string, endOfDay: boolean): Date | undefined => {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)
    if (!match) return undefined

    const year = Number(match[1])
    const month = Number(match[2]) - 1
    const day = Number(match[3])
    const date = endOfDay ? new Date(year, month, day, 23, 59, 59, 999) : new Date(year, month, day, 0, 0, 0, 0)

    if (date.getFullYear() !== year || date.getMonth() !== month || date.getDate() !== day) return undefined
    return date
}

export const createCustomTimeRange = (
    startDate: string,
    endDate: string,
    currentTime: Date = new Date()
): CustomDateRangeResult => {
    const minDate = parseLocalDate(startDate, false)
    const maxDate = parseLocalDate(endDate, true)

    if (!minDate || !maxDate) {
        const invalidFields: CustomDateRangeField[] = []
        if (!minDate) invalidFields.push('start')
        if (!maxDate) invalidFields.push('end')
        return { failure: { error: 'missing-date', invalidFields } }
    }

    if (minDate > maxDate) return { failure: { error: 'invalid-range', invalidFields: ['start', 'end'] } }

    const endOfToday = new Date(currentTime)
    endOfToday.setHours(23, 59, 59, 999)
    if (maxDate > endOfToday) return { failure: { error: 'future-date', invalidFields: ['end'] } }

    return { range: { minDate, maxDate } }
}
