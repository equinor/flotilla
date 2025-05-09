import { BackendAPICaller } from 'api/ApiCaller'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinition } from 'models/MissionDefinition'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { config } from 'config'
import { ChangeEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { EditAutoScheduleDialogContent } from './EditAutoScheduleDialogContent'
import { Button, Dialog, TextField, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { AutoScheduleFrequency } from 'models/AutoScheduleFrequency'
import { ButtonSection, FormCard } from './AutoScheduleStyledComponents'

const StyledFormCard = styled(FormCard)`
    display: flex;
`

export const StyledDialog = styled(Dialog)`
    display: flex;
    justify-content: space-between;
    padding: 1rem;
    width: auto;
    min-width: 300px;
`

interface IAutoScheduleEditDialogProps {
    missionDefinition: MissionDefinition
    fieldName: string
    closeEditDialog: () => void
}

export const AutoScheduleEditDialogContent = ({
    missionDefinition,
    fieldName,
    closeEditDialog,
}: IAutoScheduleEditDialogProps) => {
    const defaultMissionDefinitionForm: MissionDefinitionUpdateForm = {
        comment: missionDefinition.comment,
        autoScheduleFrequency: missionDefinition.autoScheduleFrequency,
        name: missionDefinition.name,
        isDeprecated: false,
    }
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const navigate = useNavigate()
    const [form, setForm] = useState<MissionDefinitionUpdateForm>(defaultMissionDefinitionForm)

    const isUpdateButtonDisabled = () => {
        if (fieldName !== 'autoScheduleFrequency') return false
        if (form.autoScheduleFrequency?.daysOfWeek.length === 0) return true
        if (form.autoScheduleFrequency?.timesOfDayCET.length === 0) return true
        return false
    }

    const handleSubmit = () => {
        BackendAPICaller.updateMissionDefinition(missionDefinition.id, form)
            .then((missionDefinition) => {
                closeEditDialog()
                if (missionDefinition.isDeprecated) navigate(`${config.FRONTEND_BASE_ROUTE}/FrontPage`)
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

    const changedAutoScheduleFrequency = (newAutoScheduleFrequency: AutoScheduleFrequency) => {
        setForm({ ...form, autoScheduleFrequency: newAutoScheduleFrequency })
    }

    const getFormItem = () => {
        switch (fieldName) {
            case 'autoScheduleFrequency':
                return (
                    <EditAutoScheduleDialogContent
                        currentAutoScheduleFrequency={missionDefinition.autoScheduleFrequency}
                        changedAutoScheduleFrequency={changedAutoScheduleFrequency}
                    />
                )
            case 'comment':
                return (
                    <TextField
                        id="commentEdit"
                        multiline
                        rows={2}
                        label={TranslateText('Comment')}
                        value={form.comment ?? ''}
                        onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                            setForm({ ...form, comment: changes.target.value })
                        }
                    />
                )
            case 'name':
                return (
                    <TextField
                        id="nameEdit"
                        value={form.name ?? ''}
                        label={TranslateText('Name')}
                        onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                            setForm({ ...form, name: changes.target.value })
                        }
                    />
                )
            default:
                console.error('Invalid field name: ', fieldName)
                break
        }
    }

    return (
        <>
            {getFormItem()}
            <ButtonSection>
                <Button onClick={closeEditDialog} variant="outlined" color="primary">
                    {TranslateText('Cancel')}
                </Button>
                <Button onClick={handleSubmit} disabled={isUpdateButtonDisabled()} variant="contained" color="primary">
                    {TranslateText('Update')}
                </Button>
            </ButtonSection>
        </>
    )
}

export const AutoScheduleEditDialog = ({
    missionDefinition,
    fieldName,
    closeEditDialog,
}: IAutoScheduleEditDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={true}>
            <StyledFormCard>
                <Typography variant="h2">{TranslateText('Edit') + ' ' + TranslateText(fieldName)}</Typography>
                <AutoScheduleEditDialogContent
                    missionDefinition={missionDefinition}
                    fieldName={fieldName}
                    closeEditDialog={closeEditDialog}
                />
            </StyledFormCard>
        </StyledDialog>
    )
}