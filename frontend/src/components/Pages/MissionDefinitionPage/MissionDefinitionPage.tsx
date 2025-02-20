import { ChangeEvent, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { MissionDefinitionHeader } from './MissionDefinitionHeader/MissionDefinitionHeader'
import { BackButton } from 'utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { Header } from 'components/Header/Header'
import { MissionDefinition } from 'models/MissionDefinition'
import { Button, Typography, TextField, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { StyledDict } from 'components/Pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledPage } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import {
    displayAutoScheduleFrequency,
    EditAutoScheduleDialogContent,
} from 'components/Pages/MissionDefinitionPage/EditAutoScheduleDialogContent'
import { AutoScheduleFrequency } from 'models/AutoScheduleFrequency'

const StyledDictCard = styled(StyledDict.Card)`
    box-shadow: ${tokens.elevation.raised};
    height: auto;
    min-height: 100px;
    overflow-y: hidden;
`

const MetadataItem = ({
    title,
    content,
    onEdit,
    onDelete,
}: {
    title: string
    content: any
    onEdit?: () => void
    onDelete?: () => void
}) => {
    return (
        <StyledDict.FormItem>
            <StyledDictCard>
                <StyledDict.TitleComponent>
                    <Typography variant="body_long_bold" color={tokens.colors.text.static_icons__secondary.rgba}>
                        {title}
                    </Typography>
                    {onEdit && (
                        <StyledDict.EditButton variant="ghost" onClick={onEdit}>
                            <Icon name={Icons.Edit} size={16} />
                        </StyledDict.EditButton>
                    )}
                    {onDelete && (
                        <StyledDict.EditButton variant="ghost" onClick={onDelete}>
                            <Icon name={Icons.Delete} size={16} />
                        </StyledDict.EditButton>
                    )}
                </StyledDict.TitleComponent>
                <Typography
                    variant="body_long"
                    group="paragraph"
                    color={tokens.colors.text.static_icons__secondary.rgba}
                >
                    {content}
                </Typography>
            </StyledDictCard>
        </StyledDict.FormItem>
    )
}

const MissionDefinitionPageBody = ({ missionDefinition }: { missionDefinition: MissionDefinition }) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
    const [selectedField, setSelectedField] = useState<string>('')
    const navigate = useNavigate()

    const displayInspectionFrequency = (inspectionFrequency: string | undefined | null) => {
        if (!inspectionFrequency) return TranslateText('No inspection frequency set')
        const timeArray = inspectionFrequency.split(':')
        const days: number = +timeArray[0] // [1] is hours and [2] is minutes
        const returnStringArray: string[] = []
        if (days > 0) returnStringArray.push(days + ' ' + TranslateText('days'))
        if (returnStringArray.length === 0) return TranslateText('No inspection frequency set')

        return TranslateText('Inspection required every') + ' ' + returnStringArray.join(', ')
    }

    const onEdit = (editType: string) => {
        return () => {
            setIsEditDialogOpen(true)
            setSelectedField(editType)
        }
    }

    const onDeleteAutoSchedule = () => {
        const defaultMissionDefinitionForm: MissionDefinitionUpdateForm = {
            comment: missionDefinition.comment,
            inspectionFrequency: missionDefinition.inspectionFrequency,
            autoScheduleFrequency: undefined,
            name: missionDefinition.name,
            isDeprecated: false,
        }
        BackendAPICaller.updateMissionDefinition(missionDefinition.id, defaultMissionDefinitionForm).catch(() => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent
                    translatedMessage={TranslateText('Failed to delete auto schedule frequency')}
                />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Failed to delete auto schedule frequency')}
                />,
                AlertCategory.ERROR
            )
        })
    }

    return (
        <>
            <StyledDict.FormContainer>
                <MetadataItem title={TranslateText('Name')} content={missionDefinition.name} onEdit={onEdit('name')} />
                <MetadataItem
                    title={TranslateText('Inspection area')}
                    content={
                        missionDefinition.inspectionArea ? missionDefinition.inspectionArea.inspectionAreaName : ''
                    }
                />
                <MetadataItem
                    title={TranslateText('Plant')}
                    content={missionDefinition.inspectionArea ? missionDefinition.inspectionArea.plantName : ''}
                />
                <MetadataItem
                    title={TranslateText('Installation')}
                    content={missionDefinition.inspectionArea ? missionDefinition.inspectionArea.installationCode : ''}
                />
                <MetadataItem title={TranslateText('Mission source id')} content={missionDefinition.sourceId} />
                <MetadataItem
                    title={TranslateText('Inspection frequency')}
                    content={displayInspectionFrequency(missionDefinition.inspectionFrequency)}
                    onEdit={onEdit('inspectionFrequency')}
                />
                <MetadataItem
                    title={TranslateText('Automated scheduling')}
                    content={displayAutoScheduleFrequency(missionDefinition.autoScheduleFrequency, TranslateText)}
                    onEdit={onEdit('autoScheduleFrequency')}
                    onDelete={onDeleteAutoSchedule}
                />
                <MetadataItem
                    title={TranslateText('Comment')}
                    content={missionDefinition.comment}
                    onEdit={onEdit('comment')}
                />
            </StyledDict.FormContainer>
            <StyledDict.Button
                disabled={!missionDefinition.lastSuccessfulRun}
                onClick={() =>
                    navigate(`${config.FRONTEND_BASE_ROUTE}/mission/${missionDefinition.lastSuccessfulRun!.id}`)
                }
            >
                {TranslateText('View last run') +
                    (missionDefinition.lastSuccessfulRun ? '' : ': ' + TranslateText('Not yet performed'))}
            </StyledDict.Button>
            {isEditDialogOpen && (
                <MissionDefinitionEditDialog
                    fieldName={selectedField}
                    missionDefinition={missionDefinition}
                    closeEditDialog={() => setIsEditDialogOpen(false)}
                />
            )}
        </>
    )
}

interface IMissionDefinitionEditDialogProps {
    missionDefinition: MissionDefinition
    fieldName: string
    closeEditDialog: () => void
}

const MissionDefinitionEditDialog = ({
    missionDefinition,
    fieldName,
    closeEditDialog,
}: IMissionDefinitionEditDialogProps) => {
    const defaultMissionDefinitionForm: MissionDefinitionUpdateForm = {
        comment: missionDefinition.comment,
        inspectionFrequency: missionDefinition.inspectionFrequency,
        autoScheduleFrequency: missionDefinition.autoScheduleFrequency,
        name: missionDefinition.name,
        isDeprecated: false,
    }
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const navigate = useNavigate()
    const [form, setForm] = useState<MissionDefinitionUpdateForm>(defaultMissionDefinitionForm)

    const updateInspectionFrequencyFormDays = (newDay: string) => {
        if (!Number(newDay) && newDay !== '') return

        const formatInspectionFrequency = (dayStr: string) => {
            dayStr = dayStr === '' ? '0' : dayStr
            if (!form.inspectionFrequency) return dayStr + '.00:00:00'

            const inspectionArray = form.inspectionFrequency.split(':')
            if (!inspectionArray || inspectionArray.length < 2) return dayStr + '.00:00:00'

            return dayStr + '.00:' + inspectionArray[1] + ':00'
        }

        setForm({ ...form, inspectionFrequency: formatInspectionFrequency(newDay) })
    }

    const getDayAndHoursFromInspectionFrequency = (inspectionFrequency: string | undefined): [number, number] => {
        if (!inspectionFrequency) return [0, 0]
        const inspectionParts = form.inspectionFrequency?.split(':')
        if (!inspectionParts || inspectionParts.length < 2) return [0, 0]
        return [+inspectionParts[0], +inspectionParts[1]]
    }

    const isUpdateButtonDisabled = () => {
        if (fieldName !== 'autoScheduleFrequency') return false
        if (form.autoScheduleFrequency?.daysOfWeek.length === 0) return true
        if (form.autoScheduleFrequency?.timesOfDay.length === 0) return true
        return false
    }

    const handleSubmit = () => {
        const daysAndHours = getDayAndHoursFromInspectionFrequency(form.inspectionFrequency)
        if (daysAndHours[0] === 0 && daysAndHours[1] === 0) form.inspectionFrequency = undefined
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

    const inspectionFrequency = getDayAndHoursFromInspectionFrequency(form.inspectionFrequency)
    const inspectionFrequencyDays =
        !inspectionFrequency[0] || inspectionFrequency[0] === 0 ? '' : String(inspectionFrequency[0])

    const changedAutoScheduleFrequency = (newAutoScheduleFrequency: AutoScheduleFrequency) => {
        setForm({ ...form, autoScheduleFrequency: newAutoScheduleFrequency })
    }

    const getFormItem = () => {
        switch (fieldName) {
            case 'inspectionFrequency':
                return (
                    <StyledDict.InspectionFrequencyDiv>
                        <TextField
                            id="compact-textfield"
                            label={TranslateText('Days between inspections')}
                            unit={TranslateText('days')}
                            value={inspectionFrequencyDays}
                            onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                                if (!Number.isNaN(+changes.target.value))
                                    updateInspectionFrequencyFormDays(changes.target.value)
                            }}
                        />
                    </StyledDict.InspectionFrequencyDiv>
                )
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
        <StyledDict.Dialog open={true}>
            <StyledDict.FormCard>
                <Typography variant="h2">{TranslateText('Edit') + ' ' + TranslateText(fieldName)}</Typography>
                {getFormItem()}
                <StyledDict.ButtonSection>
                    <Button onClick={() => closeEditDialog()} variant="outlined" color="primary">
                        {TranslateText('Cancel')}
                    </Button>
                    <Button
                        onClick={handleSubmit}
                        disabled={isUpdateButtonDisabled()}
                        variant="contained"
                        color="primary"
                    >
                        {TranslateText('Update')}
                    </Button>
                </StyledDict.ButtonSection>
            </StyledDict.FormCard>
        </StyledDict.Dialog>
    )
}

export const MissionDefinitionPage = () => {
    const { missionId } = useParams()
    const { missionDefinitions } = useMissionDefinitionsContext()

    const selectedMissionDefinition = missionDefinitions.find((m) => m.id === missionId)

    return (
        <>
            <Header page={'mission'} />
            {selectedMissionDefinition !== undefined && (
                <StyledPage>
                    <BackButton />
                    <MissionDefinitionHeader missionDefinition={selectedMissionDefinition} />
                    <MissionDefinitionPageBody missionDefinition={selectedMissionDefinition} />
                </StyledPage>
            )}
        </>
    )
}
