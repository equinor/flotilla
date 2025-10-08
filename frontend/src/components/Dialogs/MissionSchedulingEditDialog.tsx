import { Button, Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    displayAutoScheduleFrequency,
    SelectDaysOfWeek,
    SelectTimesOfDay,
    StyledSummary,
} from 'components/Pages/MissionDefinitionPage/EditAutoScheduleDialogContent'
import { ButtonSection, FormCard } from 'components/Pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { config } from 'config'
import { DaysOfWeek } from 'models/AutoScheduleFrequency'
import { MissionDefinition } from 'models/MissionDefinition'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

interface MissionSchedulingEditDialogProps {
    mission: MissionDefinition
    isOpen: boolean
    onClose: () => void
}

export const MissionSchedulingEditDialog = ({ mission, isOpen, onClose }: MissionSchedulingEditDialogProps) => {
    const { TranslateText } = useLanguageContext()

    const [days, setDays] = useState<DaysOfWeek[]>(mission.autoScheduleFrequency?.daysOfWeek ?? [])
    const [timesOfDay, setTimesOfDay] = useState<string[]>(mission.autoScheduleFrequency?.timesOfDayCET ?? [])

    const { setAlert, setListAlert } = useAlertContext()
    const navigate = useNavigate()

    const handleSubmit = () => {
        const form: MissionDefinitionUpdateForm = {
            comment: mission.comment,
            autoScheduleFrequency: {
                daysOfWeek: days,
                timesOfDayCET: timesOfDay,
            },
            name: mission.name,
            isDeprecated: false,
        }
        BackendAPICaller.updateMissionDefinition(mission.id, form)
            .then((missionDefinition) => {
                onClose()
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
                                {displayAutoScheduleFrequency(
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
                            onClick={handleSubmit}
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
