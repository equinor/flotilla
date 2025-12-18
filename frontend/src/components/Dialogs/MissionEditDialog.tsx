import { Button, Checkbox, Chip, Textarea, TextField, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { BackendAPICaller } from 'api/ApiCaller'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    ButtonSection,
    FormCard,
    StyledDialog as MissionStyledDialog,
} from 'pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { config } from 'config'
import { allDays, DaysOfWeek, TimeAndDay } from 'models/AutoScheduleFrequency'
import { MissionDefinition } from 'models/MissionDefinition'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { ChangeEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import styled from 'styled-components'
import { useAssetContext } from 'components/Contexts/AssetContext'

const StyledSummary = styled.div`
    padding: 16px 8px 0px 8px;
    border-top: 1px solid ${tokens.colors.interactive.disabled__border.hex};
`

const StyledFormCard = styled(FormCard)`
    max-width: 340px;
`

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

const useMissionUpdater = () => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const { installationCode } = useAssetContext()
    const navigate = useNavigate()

    const updateMission = (
        mission: MissionDefinition,
        partialForm: Partial<MissionDefinitionUpdateForm>,
        onSuccess: () => void
    ) => {
        const defaultForm: MissionDefinitionUpdateForm = {
            comment: mission.comment,
            schedulingTimesCETperWeek: mission.autoScheduleFrequency?.schedulingTimesCETperWeek,
            name: mission.name,
            isDeprecated: false,
        }
        const form: MissionDefinitionUpdateForm = { ...defaultForm, ...partialForm }

        BackendAPICaller.updateMissionDefinition(mission.id, form)
            .then((missionDefinition) => {
                onSuccess()
                if (missionDefinition.isDeprecated)
                    navigate(`${config.FRONTEND_BASE_ROUTE}/${installationCode}:front-page`)
            })
            .catch(() => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to update inspection')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent translatedMessage={TranslateText('Failed to update inspection')} />,
                    AlertCategory.ERROR
                )
            })
    }

    return updateMission
}

interface MissionEditDialogProps {
    mission: MissionDefinition
    isOpen: boolean
    onClose: () => void
}

export const MissionSchedulingEditDialog = ({ mission, isOpen, onClose }: MissionEditDialogProps) => {
    const { TranslateText } = useLanguageContext()
    const updateMission = useMissionUpdater()

    const [schedulingTimes, setSchedulingTimes] = useState<TimeAndDay[]>(
        mission.autoScheduleFrequency?.schedulingTimesCETperWeek ?? []
    )

    const onSubmit = () => {
        updateMission(
            mission,
            {
                schedulingTimesCETperWeek: schedulingTimes,
            },
            onClose
        )
    }

    const removeTimeAndDay = (time: string, day: DaysOfWeek) => {
        setSchedulingTimes(schedulingTimes.filter((t) => !(t.timeOfDay === time && t.dayOfWeek === day)))
    }

    return (
        <>
            <StyledDialog open={isOpen} isDismissable={true} onClose={onClose}>
                <StyledDialog.Header>
                    <StyledDialog.Title>
                        <Typography variant="h3">
                            {TranslateText('Edit auto scheduling of mission') + ' ' + mission.name}
                        </Typography>
                    </StyledDialog.Title>
                </StyledDialog.Header>
                <FormCard>
                    <Typography variant="h6">{TranslateText('Add additional times')}</Typography>

                    <SelectTimesAndDates
                        currentAutoScheduleTimes={schedulingTimes}
                        changedAutoScheduleTimes={setSchedulingTimes}
                    />
                    <SelectedTimeDaySummary schedulingTimes={schedulingTimes} removeTimeAndDay={removeTimeAndDay} />
                    <ButtonSection>
                        <Button onClick={onClose} variant="outlined" color="primary">
                            {TranslateText('Cancel')}
                        </Button>
                        <Button
                            onClick={onSubmit}
                            disabled={schedulingTimes.length === 0}
                            variant="contained"
                            color="primary"
                        >
                            {TranslateText('Update')}
                        </Button>
                    </ButtonSection>
                </FormCard>
            </StyledDialog>
        </>
    )
}

export const MissionNameEditDialog = ({ mission, isOpen, onClose }: MissionEditDialogProps) => {
    const { TranslateText } = useLanguageContext()
    const updateMission = useMissionUpdater()

    const [name, setName] = useState<string>(mission.name)

    const onSubmit = () => {
        updateMission(mission, { name: name }, onClose)
    }

    return (
        <MissionStyledDialog open={isOpen}>
            <StyledFormCard>
                <Typography variant="h2">{TranslateText('Edit') + ' ' + TranslateText('name')}</Typography>
                <TextField
                    id="nameEdit"
                    value={name}
                    label={TranslateText('Name')}
                    onChange={(changes: ChangeEvent<HTMLInputElement>) => setName(changes.target.value)}
                />
                <ButtonSection>
                    <Button onClick={onClose} variant="outlined" color="primary">
                        {TranslateText('Cancel')}
                    </Button>
                    <Button onClick={onSubmit} disabled={!name} variant="contained" color="primary">
                        {TranslateText('Update')}
                    </Button>
                </ButtonSection>
            </StyledFormCard>
        </MissionStyledDialog>
    )
}

export const MissionCommentEditDialog = ({ mission, isOpen, onClose }: MissionEditDialogProps) => {
    const { TranslateText } = useLanguageContext()
    const updateMission = useMissionUpdater()

    const [comment, setComment] = useState<string>(mission.comment ?? '')

    const onSubmit = () => {
        updateMission(mission, { comment: comment }, onClose)
    }

    return (
        <MissionStyledDialog open={isOpen}>
            <StyledFormCard>
                <Typography variant="h2">{TranslateText('Edit') + ' ' + TranslateText('comment')}</Typography>
                <Textarea
                    id="commentEdit"
                    rows={2}
                    label={TranslateText('Comment')}
                    value={comment}
                    onChange={(changes: ChangeEvent<HTMLTextAreaElement>) => setComment(changes.target.value)}
                />
                <ButtonSection>
                    <Button onClick={onClose} variant="outlined" color="primary">
                        {TranslateText('Cancel')}
                    </Button>
                    <Button onClick={onSubmit} variant="contained" color="primary">
                        {TranslateText('Update')}
                    </Button>
                </ButtonSection>
            </StyledFormCard>
        </MissionStyledDialog>
    )
}

interface SelectDaysOfWeekProps {
    currentAutoScheduleDays: DaysOfWeek[]
    changedAutoScheduleDays: (newAutoScheduleDays: DaysOfWeek[]) => void
}

const SelectDaysOfWeek = ({ currentAutoScheduleDays, changedAutoScheduleDays }: SelectDaysOfWeekProps) => {
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
            <Typography variant="meta">{TranslateText('Select days of the week')}</Typography>
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

const SelectTimesAndDates = ({
    currentAutoScheduleTimes,
    changedAutoScheduleTimes,
}: {
    currentAutoScheduleTimes: TimeAndDay[]
    changedAutoScheduleTimes: (newTimes: TimeAndDay[]) => void
}) => {
    const addTime = (newTimes: TimeAndDay[]) => {
        const newAutoScheduleTimes = [...currentAutoScheduleTimes]
        newTimes.forEach((t) => {
            if (
                !newAutoScheduleTimes.find(
                    (existingTime) => existingTime.dayOfWeek === t.dayOfWeek && existingTime.timeOfDay === t.timeOfDay
                )
            ) {
                newAutoScheduleTimes.push(t)
            }
        })
        changedAutoScheduleTimes(newAutoScheduleTimes)
    }

    return (
        <>
            <AddTimesAndDates addAutoScheduleTimes={addTime} />
        </>
    )
}

const AddTimesAndDates = ({ addAutoScheduleTimes }: { addAutoScheduleTimes: (newTimes: TimeAndDay[]) => void }) => {
    const { TranslateText } = useLanguageContext()

    const [selectedDays, setSelectedDays] = useState<DaysOfWeek[]>([])
    const [selectedTime, setSelectedTime] = useState<string>() // Format HH:MM:ss

    const addTime = () => {
        if (selectedTime === undefined || selectedDays.length === 0) return
        addAutoScheduleTimes(selectedDays.map((day) => ({ dayOfWeek: day, timeOfDay: selectedTime })))
    }

    return (
        <>
            <SelectDaysOfWeek currentAutoScheduleDays={selectedDays} changedAutoScheduleDays={setSelectedDays} />
            <SelectTimeOfDay changedAutoScheduleTime={setSelectedTime} />
            <Button onClick={addTime} disabled={selectedTime === undefined || selectedDays.length === 0}>
                {TranslateText('Add time')}
            </Button>
        </>
    )
}

const SelectTimeOfDay = ({ changedAutoScheduleTime }: { changedAutoScheduleTime: (newTime: string) => void }) => {
    const { TranslateText } = useLanguageContext()

    const formatAsTimeOnly = (time: string) => {
        return time.concat(':00')
    }

    return (
        <StyledSelectSection>
            <Typography variant="meta">{TranslateText('Select start time')}</Typography>
            <StyledTimeSelector>
                <TextField
                    id="time"
                    type="time"
                    onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                        changedAutoScheduleTime(formatAsTimeOnly(changes.target.value))
                    }
                />
            </StyledTimeSelector>
        </StyledSelectSection>
    )
}

const SelectedTimeDaySummary = ({
    schedulingTimes,
    removeTimeAndDay,
}: {
    schedulingTimes: TimeAndDay[]
    removeTimeAndDay: (time: string, day: DaysOfWeek) => void
}) => {
    const { TranslateText } = useLanguageContext()

    const timesForSpecificDay = (day: DaysOfWeek) => {
        return schedulingTimes.filter((t) => t.dayOfWeek === day)
    }

    return (
        <StyledSummary>
            <Typography variant="h6">{TranslateText('Selected times')}</Typography>
            {schedulingTimes.length === 0 && (
                <Typography color="warning">{TranslateText('No times have been selected. Please add time')}</Typography>
            )}
            {allDays.map((day) => (
                <div key={`Summary ${day}`}>
                    {timesForSpecificDay(day).length > 0 && (
                        <>
                            <Typography>{TranslateText(day) + ':'}</Typography>
                            <StyledTimeChips>
                                {timesForSpecificDay(day).map((timeAndDay) => (
                                    <SelectedTimeChip
                                        key={day + timeAndDay.timeOfDay}
                                        time={timeAndDay.timeOfDay}
                                        remove={() => {
                                            removeTimeAndDay(timeAndDay.timeOfDay, timeAndDay.dayOfWeek)
                                        }}
                                    />
                                ))}
                            </StyledTimeChips>
                        </>
                    )}
                </div>
            ))}
        </StyledSummary>
    )
}

const SelectedTimeChip = ({ time, remove }: { time: string; remove: () => void }) => {
    const formatTimeToDisplay = (time: string) => {
        return time.substring(0, 5)
    }
    return (
        <Chip key={time} onDelete={() => remove()}>
            {formatTimeToDisplay(time)}
        </Chip>
    )
}
