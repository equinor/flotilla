import styled from 'styled-components'
import { ChangeEvent, useEffect, useState } from 'react'
import { Button, Checkbox, Chip, TextField, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { AutoScheduleFrequency, DaysOfWeek } from 'models/AutoScheduleFrequency'
import { allDays } from '../FrontPage/AutoScheduleSection/AutoScheduleSection'

const StyledSelectSection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 8px;
`
const StyledDaySelector = styled.div`
    display: flex;
    flex-direction: row;
    gap: 2px;
`
const DayButton = styled(Button)`
    width: 42px;
`
const StyledTimeSelector = styled.div`
    display: flex;
    flex-direction: row;
    align-items: end;
    gap: 8px;
`
const StyledTimeChips = styled.div`
    display: flex;
    flex-direction: row;
    gap: 8px;
`
const StyledSummary = styled.div`
    padding: 16px 8px 0px 8px;
    border-top: 1px solid ${tokens.colors.interactive.disabled__border.hex};
`

interface EditAutoScheduleDaysProps {
    currentAutoScheduleDays: DaysOfWeek[]
    changedAutoScheduleDays: (newAutoScheduleDays: DaysOfWeek[]) => void
}
interface EditAutoScheduleTimesProps {
    currentAutoScheduleTimes: string[]
    changedAutoScheduleTimes: (newAutoScheduleTimes: string[]) => void
}
interface EditAutoScheduleDialogContentProps {
    currentAutoScheduleFrequency?: AutoScheduleFrequency
    changedAutoScheduleFrequency: (newAutoScheduleFrequency: AutoScheduleFrequency) => void
}

const SelectDaysOfWeek = ({ currentAutoScheduleDays, changedAutoScheduleDays }: EditAutoScheduleDaysProps) => {
    const { TranslateText } = useLanguageContext()

    const isAllSelected = currentAutoScheduleDays?.length === allDays.length

    const selectAll = () => {
        changedAutoScheduleDays(allDays)
    }

    const unselectAll = () => {
        changedAutoScheduleDays([])
    }

    const addDay = (day: DaysOfWeek) => {
        if (currentAutoScheduleDays?.includes(day)) return
        changedAutoScheduleDays([...currentAutoScheduleDays, day])
    }

    const removeDay = (day: DaysOfWeek) => {
        changedAutoScheduleDays(currentAutoScheduleDays.filter((d) => d !== day))
    }

    const OneLetterDayButton = ({ day }: { day: DaysOfWeek }) => {
        const isDaySelected = currentAutoScheduleDays.includes(day)
        return (
            <DayButton
                variant={isDaySelected ? 'contained' : 'outlined'}
                onClick={() => (isDaySelected ? removeDay(day) : addDay(day))}
            >
                {TranslateText('short'.concat(day.toString()))}
            </DayButton>
        )
    }

    return (
        <StyledSelectSection>
            <Typography variant="h6">{TranslateText('Select days of the week')}</Typography>
            <StyledDaySelector>
                {Object.entries(allDays).map(([key, value]) => (
                    <OneLetterDayButton key={key} day={value} />
                ))}
            </StyledDaySelector>
            <Checkbox
                label={TranslateText('Select all days')}
                checked={isAllSelected}
                onChange={isAllSelected ? unselectAll : selectAll}
            />
        </StyledSelectSection>
    )
}

const SelectTimesOfDay = ({ currentAutoScheduleTimes, changedAutoScheduleTimes }: EditAutoScheduleTimesProps) => {
    const { TranslateText } = useLanguageContext()
    const [selectedTime, setSelectedTime] = useState<string>() // Format HH:MM:ss

    const removeTime = (time: string) => {
        changedAutoScheduleTimes([...currentAutoScheduleTimes.filter((t) => t !== time)])
    }

    const addTime = () => {
        if (!selectedTime) return

        if (selectedTime && !currentAutoScheduleTimes.includes(selectedTime))
            changedAutoScheduleTimes([...currentAutoScheduleTimes, selectedTime])
    }

    const formatTimeToDisplay = (time: string) => {
        return time.substring(0, 5)
    }

    const formatAsTimeOnly = (time: string) => {
        return time.concat(':00')
    }

    const SelectedTimeChips = () => (
        <StyledTimeChips>
            {currentAutoScheduleTimes
                .sort((a, b) => {
                    if (a > b) return 1
                    return -1
                })
                .map((time) => (
                    <Chip key={time} onDelete={() => removeTime(time)}>
                        {formatTimeToDisplay(time)}
                    </Chip>
                ))}
        </StyledTimeChips>
    )

    return (
        <StyledSelectSection>
            <Typography variant="h6"> {TranslateText('Select times of the day')}</Typography>
            <Typography variant="meta">{TranslateText('Add start time')}</Typography>
            <StyledTimeSelector>
                <TextField
                    id="time"
                    type="time"
                    onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                        setSelectedTime(formatAsTimeOnly(changes.target.value))
                    }
                />
                <Button onClick={addTime} disabled={selectedTime === undefined}>
                    {TranslateText('Add time')}
                </Button>
            </StyledTimeSelector>
            <Typography>{TranslateText('Selected start times')}</Typography>
            <SelectedTimeChips />
        </StyledSelectSection>
    )
}

export const displayAutoScheduleFrequency = (
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

export const EditAutoScheduleDialogContent = ({
    currentAutoScheduleFrequency,
    changedAutoScheduleFrequency,
}: EditAutoScheduleDialogContentProps) => {
    const { TranslateText } = useLanguageContext()
    const [currentAutoSetDays, setCurrentAutoSetDays] = useState<DaysOfWeek[]>(
        currentAutoScheduleFrequency?.daysOfWeek ?? []
    )
    const [currentAutoSetTimes, setCurrentAutoSetTimes] = useState<string[]>(
        currentAutoScheduleFrequency?.timesOfDayCET ?? []
    )

    useEffect(() => {
        changedAutoScheduleFrequency({
            daysOfWeek: currentAutoSetDays,
            timesOfDayCET: currentAutoSetTimes,
            autoScheduledJobs: currentAutoScheduleFrequency?.autoScheduledJobs,
        })
    }, [currentAutoSetDays, currentAutoSetTimes])

    return (
        <>
            <SelectDaysOfWeek
                currentAutoScheduleDays={currentAutoSetDays}
                changedAutoScheduleDays={setCurrentAutoSetDays}
            />
            <SelectTimesOfDay
                currentAutoScheduleTimes={currentAutoSetTimes}
                changedAutoScheduleTimes={setCurrentAutoSetTimes}
            />
            <StyledSummary>
                {currentAutoSetDays.length === 0 && (
                    <Typography color="warning">
                        {TranslateText('No days have been selected. Please select days')}
                    </Typography>
                )}
                {currentAutoSetTimes.length === 0 && (
                    <Typography color="warning">
                        {TranslateText('No times have been selected. Please add time')}
                    </Typography>
                )}
                {currentAutoSetDays.length > 0 && currentAutoSetTimes.length > 0 && (
                    <Typography>
                        {displayAutoScheduleFrequency(
                            { daysOfWeek: currentAutoSetDays, timesOfDayCET: currentAutoSetTimes },
                            TranslateText
                        )}
                    </Typography>
                )}
            </StyledSummary>
        </>
    )
}
