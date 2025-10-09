import { AutoScheduleFrequency, DaysOfWeek } from 'models/AutoScheduleFrequency'

export const formulateAutoScheduleFrequencyAsString = (
    autoScheduleFrequency: AutoScheduleFrequency | undefined,
    translateText: (str: string, args?: string[]) => string
) => {
    if (!autoScheduleFrequency || !autoScheduleFrequency.timesOfDayCET)
        return translateText('No automated scheduling set')

    const formatListToSentence = (list: string[]) => {
        if (list.length === 1) return list[0]
        return list.slice(0, -1).join(', ') + ' ' + translateText('and') + ' ' + list.slice(-1)
    }

    const sortedDays = (days: DaysOfWeek[]) => {
        return days.sort((a, b) => Object.keys(DaysOfWeek).indexOf(a) - Object.keys(DaysOfWeek).indexOf(b))
    }

    let formattedDays = ''
    if (autoScheduleFrequency.daysOfWeek.length === 7 || !autoScheduleFrequency.daysOfWeek)
        formattedDays = translateText('day')
    else {
        formattedDays = formatListToSentence(
            sortedDays(autoScheduleFrequency.daysOfWeek).map((day) => translateText(day.toString()))
        )
    }

    const timesOfDay = formatListToSentence(autoScheduleFrequency.timesOfDayCET.map((time) => time.substring(0, 5)))

    return translateText('Scheduled every {0} at {1}', [formattedDays, timesOfDay])
}
