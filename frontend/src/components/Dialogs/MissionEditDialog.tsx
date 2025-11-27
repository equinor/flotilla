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
import { allDays, DaysOfWeek } from 'models/AutoScheduleFrequency'
import { MissionDefinition } from 'models/MissionDefinition'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { ChangeEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import styled from 'styled-components'
import { formulateAutoScheduleFrequencyAsString } from 'utils/language'

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
    const navigate = useNavigate()

    const updateMission = (
        mission: MissionDefinition,
        partialForm: Partial<MissionDefinitionUpdateForm>,
        onSuccess: () => void
    ) => {
        const defaultForm: MissionDefinitionUpdateForm = {
            comment: mission.comment,
            autoScheduleFrequency: mission.autoScheduleFrequency,
            name: mission.name,
            isDeprecated: false,
        }
        const form: MissionDefinitionUpdateForm = { ...defaultForm, ...partialForm }

        BackendAPICaller.updateMissionDefinition(mission.id, form)
            .then((missionDefinition) => {
                onSuccess()
                if (missionDefinition.isDeprecated) navigate(`${config.FRONTEND_BASE_ROUTE}/front-page`)
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

    const [days, setDays] = useState<DaysOfWeek[]>(mission.autoScheduleFrequency?.daysOfWeek ?? [])
    const [timesOfDay, setTimesOfDay] = useState<string[]>(mission.autoScheduleFrequency?.timesOfDayCET ?? [])

    const onSubmit = () => {
        updateMission(
            mission,
            {
                autoScheduleFrequency: {
                    daysOfWeek: days,
                    timesOfDayCET: timesOfDay,
                },
            },
            onClose
        )
    }

    return (
        <>
            <StyledDialog open={isOpen}>
                <StyledDialog.Header>
                    <StyledDialog.Title>
                        <Typography variant="h3">
                            {TranslateText('Edit auto scheduling of mission') + ' ' + mission.name}
                        </Typography>
                    </StyledDialog.Title>
                </StyledDialog.Header>
                <FormCard>
                    <SelectDaysOfWeek currentAutoScheduleDays={days} changedAutoScheduleDays={setDays} />
                    <SelectTimesOfDay currentAutoScheduleTimes={timesOfDay} changedAutoScheduleTimes={setTimesOfDay} />
                    <StyledSummary>
                        {days.length === 0 && (
                            <Typography color="warning">
                                {TranslateText('No days have been selected. Please select days')}
                            </Typography>
                        )}
                        {timesOfDay.length === 0 && (
                            <Typography color="warning">
                                {TranslateText('No times have been selected. Please add time')}
                            </Typography>
                        )}
                        {days.length > 0 && timesOfDay.length > 0 && (
                            <Typography>
                                {formulateAutoScheduleFrequencyAsString(
                                    { daysOfWeek: days, timesOfDayCET: timesOfDay },
                                    TranslateText
                                )}
                            </Typography>
                        )}
                    </StyledSummary>
                    <ButtonSection>
                        <Button onClick={onClose} variant="outlined" color="primary">
                            {TranslateText('Cancel')}
                        </Button>
                        <Button
                            onClick={onSubmit}
                            disabled={days.length === 0 || timesOfDay.length === 0}
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

interface SelectTimesOfDayProps {
    currentAutoScheduleTimes: string[]
    changedAutoScheduleTimes: (newAutoScheduleTimes: string[]) => void
}

const SelectTimesOfDay = ({ currentAutoScheduleTimes, changedAutoScheduleTimes }: SelectTimesOfDayProps) => {
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
                .sort((a, b) => (a === b ? 0 : a > b ? 1 : -1))
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
