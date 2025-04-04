import { ChangeEvent, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { MissionDefinitionHeader } from './MissionDefinitionHeader/MissionDefinitionHeader'
import { BackButton } from 'utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { Header } from 'components/Header/Header'
import { MissionDefinition } from 'models/MissionDefinition'
import { Button, Typography, TextField, Icon, Card } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
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
import { TaskTableAndMap } from '../MissionPage/TaskTableAndMap'
import { useQuery } from '@tanstack/react-query'
import {
    ButtonSection,
    EditButton,
    FormCard,
    FormContainer,
    FormItem,
    StyledDialog,
    TitleComponent,
} from './MissionDefinitionStyledComponents'

const StyledFormCard = styled(FormCard)`
    max-width: 340px;
`

const StyledCard = styled(Card)`
    display: flex;
    padding: 8px;
    min-height: 100px;
    height: auto
    border-radius: 6px;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    overflow-y: hidden;
`
const StyledTableAndMap = styled.div`
    display: flex;
    width: fit-content;
`

const StyledTopComponents = styled.div`
    display: flex;
    justify-content: left;
    gap: 30px;
`
const StyledButton = styled(Button)`
    width: 160px;
`
const StyledMissionDefinitionPageBody = styled.div`
    display: flex;
    flex-direction: column;
    justify-items: stretch;
    gap: 30px;
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
        <FormItem>
            <StyledCard>
                <TitleComponent>
                    <Typography variant="body_long_bold" color={tokens.colors.text.static_icons__secondary.hex}>
                        {title}
                    </Typography>
                    {onEdit && (
                        <EditButton variant="ghost" onClick={onEdit}>
                            <Icon name={Icons.Edit} size={16} />
                        </EditButton>
                    )}
                    {onDelete && (
                        <EditButton variant="ghost" onClick={onDelete}>
                            <Icon name={Icons.Delete} size={16} />
                        </EditButton>
                    )}
                </TitleComponent>
                <Typography
                    variant="body_long"
                    group="paragraph"
                    color={tokens.colors.text.static_icons__secondary.hex}
                >
                    {content}
                </Typography>
            </StyledCard>
        </FormItem>
    )
}

const MissionDefinitionPageBody = ({ missionDefinition }: { missionDefinition: MissionDefinition }) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
    const [selectedField, setSelectedField] = useState<string>('')

    const lastMissionRun = useQuery({
        queryKey: ['fetchMissionRun', missionDefinition.lastSuccessfulRun?.id],
        queryFn: async () => {
            return await BackendAPICaller.getMissionRunById(missionDefinition.lastSuccessfulRun!.id)
        },
        retry: 2,
        retryDelay: 2000,
        enabled: missionDefinition.lastSuccessfulRun?.id !== undefined,
    }).data

    const onEdit = (editType: string) => {
        return () => {
            setIsEditDialogOpen(true)
            setSelectedField(editType)
        }
    }

    const onDeleteAutoSchedule = () => {
        const defaultMissionDefinitionForm: MissionDefinitionUpdateForm = {
            comment: missionDefinition.comment,
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
        <StyledMissionDefinitionPageBody>
            <FormContainer>
                <MetadataItem title={TranslateText('Name')} content={missionDefinition.name} onEdit={onEdit('name')} />
                <MetadataItem
                    title={TranslateText('Inspection area')}
                    content={
                        missionDefinition.inspectionArea ? missionDefinition.inspectionArea.inspectionAreaName : ''
                    }
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
            </FormContainer>

            {isEditDialogOpen && (
                <MissionDefinitionEditDialog
                    fieldName={selectedField}
                    missionDefinition={missionDefinition}
                    closeEditDialog={() => setIsEditDialogOpen(false)}
                />
            )}
            <StyledTableAndMap>
                {lastMissionRun && <TaskTableAndMap mission={lastMissionRun} missionDefinitionPage={true} />}
            </StyledTableAndMap>
        </StyledMissionDefinitionPageBody>
    )
}

interface IMissionDefinitionEditDialogProps {
    missionDefinition: MissionDefinition
    fieldName: string
    closeEditDialog: () => void
}

export const MissionDefinitionEditDialogContent = ({
    missionDefinition,
    fieldName,
    closeEditDialog,
}: IMissionDefinitionEditDialogProps) => {
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

const MissionDefinitionEditDialog = ({
    missionDefinition,
    fieldName,
    closeEditDialog,
}: IMissionDefinitionEditDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={true}>
            <StyledFormCard>
                <Typography variant="h2">{TranslateText('Edit') + ' ' + TranslateText(fieldName)}</Typography>
                <MissionDefinitionEditDialogContent
                    missionDefinition={missionDefinition}
                    fieldName={fieldName}
                    closeEditDialog={closeEditDialog}
                />
            </StyledFormCard>
        </StyledDialog>
    )
}

export const MissionDefinitionPage = () => {
    const { missionId } = useParams()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()

    const selectedMissionDefinition = missionDefinitions.find((m) => m.id === missionId)

    return (
        <>
            <Header page={'mission'} />
            {selectedMissionDefinition !== undefined && (
                <StyledPage>
                    <BackButton />
                    <StyledTopComponents>
                        <MissionDefinitionHeader missionDefinition={selectedMissionDefinition} />
                        <StyledButton
                            variant="outlined"
                            disabled={!selectedMissionDefinition.lastSuccessfulRun}
                            onClick={() =>
                                navigate(
                                    `${config.FRONTEND_BASE_ROUTE}/mission/${selectedMissionDefinition.lastSuccessfulRun!.id}`
                                )
                            }
                        >
                            {TranslateText('View last run') +
                                (selectedMissionDefinition.lastSuccessfulRun
                                    ? ''
                                    : ': ' + TranslateText('Not yet performed'))}
                        </StyledButton>
                    </StyledTopComponents>
                    <MissionDefinitionPageBody missionDefinition={selectedMissionDefinition} />
                </StyledPage>
            )}
        </>
    )
}
