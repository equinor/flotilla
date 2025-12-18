import { allDays, AutoScheduleFrequency, DaysOfWeek } from 'models/AutoScheduleFrequency'

export const formulateAutoScheduleFrequencyAsString = (
    autoScheduleFrequency: AutoScheduleFrequency | undefined,
    translateText: (str: string, args?: string[]) => string
) => {
    if (!autoScheduleFrequency || !autoScheduleFrequency.schedulingTimesCETperWeek)
        return translateText('No automated scheduling set')

    const formatListToSentence = (list: string[]) => {
        if (list.length === 1) return list[0]
        return list.slice(0, -1).join(', ') + ' ' + translateText('and') + ' ' + list.slice(-1)
    }

    const sortedSchedulingTimes = autoScheduleFrequency.schedulingTimesCETperWeek.sort((a, b) => {
        const dayComparison =
            Object.keys(DaysOfWeek).indexOf(a.dayOfWeek.toString()) -
            Object.keys(DaysOfWeek).indexOf(b.dayOfWeek.toString())
        if (dayComparison !== 0) return dayComparison
        return a.timeOfDay.localeCompare(b.timeOfDay)
    })

    const timesForSpecificDay = (day: DaysOfWeek) => {
        return sortedSchedulingTimes.filter((t) => t.dayOfWeek === day)
    }

    const formatedDaysWithTimes = allDays
        .map((day) => {
            const times = timesForSpecificDay(day)
            if (times.length === 0) return null
            return translateText(day) + ' ' + formatListToSentence(times.map((t) => t.timeOfDay.substring(0, 5)))
        })
        .filter((str) => str !== null)
        .join('; ')

    return translateText('Scheduled every') + ' ' + formatedDaysWithTimes
}
