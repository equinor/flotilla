import { ChangeEvent, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { MissionDefinitionHeader } from './MissionDefinitionHeader/MissionDefinitionHeader'
import { BackButton } from '../../../utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { Header } from 'components/Header/Header'
import { CondensedMissionDefinition, SourceType } from 'models/MissionDefinition'
import { Button, Typography, TextField, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { config } from 'config'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { StyledDict } from './MissionDefinitionStyledComponents'

const MetadataItem = ({ title, content, onEdit }: { title: string; content: any; onEdit?: () => void }) => {
    return (
        <StyledDict.FormItem>
            <StyledDict.Card style={{ boxShadow: tokens.elevation.raised }}>
                <StyledDict.TitleComponent>
                    <Typography variant="body_long_bold" color={tokens.colors.text.static_icons__secondary.rgba}>
                        {title}
                    </Typography>
                    {onEdit && (
                        <StyledDict.EditButton variant="ghost" onClick={onEdit}>
                            <Icon name={Icons.Edit} size={16} />
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
            </StyledDict.Card>
        </StyledDict.FormItem>
    )
}

interface IMissionDefinitionPageBodyProps {
    missionDefinition: CondensedMissionDefinition
    updateMissionDefinition: (missionDefinition: CondensedMissionDefinition) => void
}

const MissionDefinitionPageBody = ({ missionDefinition, updateMissionDefinition }: IMissionDefinitionPageBodyProps) => {
    const { TranslateText } = useLanguageContext()
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
    const [selectedField, setSelectedField] = useState<string>('')
    let navigate = useNavigate()

    const displayInspectionFrequency = (inspectionFrequency: string | undefined | null) => {
        if (!inspectionFrequency) return TranslateText('No inspection frequency set')
        const timeArray = inspectionFrequency.split(':')
        const days: number = +timeArray[0] // [1] is hours and [2] is minutes
        let returnStringArray: string[] = []
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

    return (
        <>
            <StyledDict.FormContainer>
                <MetadataItem title={TranslateText('Name')} content={missionDefinition.name} onEdit={onEdit('name')} />
                <MetadataItem
                    title={TranslateText('Area')}
                    content={missionDefinition.area ? missionDefinition.area.areaName : ''}
                />
                <MetadataItem
                    title={TranslateText('Deck')}
                    content={missionDefinition.area ? missionDefinition.area.deckName : ''}
                />
                <MetadataItem
                    title={TranslateText('Plant')}
                    content={missionDefinition.area ? missionDefinition.area.plantName : ''}
                />
                <MetadataItem
                    title={TranslateText('Installation')}
                    content={missionDefinition.area ? missionDefinition.area.installationCode : ''}
                />
                <MetadataItem
                    title={TranslateText('Mission source')}
                    content={TranslateText(missionDefinition.sourceType)}
                />
                <MetadataItem
                    title={TranslateText('Inspection frequency')}
                    content={displayInspectionFrequency(missionDefinition.inspectionFrequency)}
                    onEdit={onEdit('inspectionFrequency')}
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
                    updateMissionDefinition={updateMissionDefinition}
                />
            )}
        </>
    )
}

interface IMissionDefinitionEditDialogProps {
    missionDefinition: CondensedMissionDefinition
    fieldName: string
    closeEditDialog: () => void
    updateMissionDefinition: (missionDefinition: CondensedMissionDefinition) => void
}

const MissionDefinitionEditDialog = ({
    missionDefinition,
    updateMissionDefinition,
    fieldName,
    closeEditDialog,
}: IMissionDefinitionEditDialogProps) => {
    const defaultMissionDefinitionForm: MissionDefinitionUpdateForm = {
        comment: missionDefinition.comment,
        inspectionFrequency: missionDefinition.inspectionFrequency,
        name: missionDefinition.name,
        isDeprecated: false,
    }
    const { TranslateText } = useLanguageContext()
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

    const handleSubmit = () => {
        const daysAndHours = getDayAndHoursFromInspectionFrequency(form.inspectionFrequency)
        if (daysAndHours[0] === 0 && daysAndHours[1] === 0) form.inspectionFrequency = undefined
        BackendAPICaller.updateMissionDefinition(missionDefinition.id, form).then((missionDefinition) => {
            closeEditDialog()
            // When we integrate signalR, we will no longer need this function call as it will update regardless
            if (missionDefinition.isDeprecated) updateMissionDefinition(missionDefinition)
        })
    }

    const inspectionFrequency = getDayAndHoursFromInspectionFrequency(form.inspectionFrequency)
    const inspectionFrequencyDays =
        !inspectionFrequency[0] || inspectionFrequency[0] === 0 ? '' : String(inspectionFrequency[0])

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
                                if (!isNaN(+changes.target.value))
                                    updateInspectionFrequencyFormDays(changes.target.value)
                            }}
                        />
                    </StyledDict.InspectionFrequencyDiv>
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
                    <Button onClick={handleSubmit} variant="contained" color="primary">
                        {TranslateText('Update')}
                    </Button>
                </StyledDict.ButtonSection>
            </StyledDict.FormCard>
        </StyledDict.Dialog>
    )
}

export const MissionDefinitionPage = () => {
    const { missionId } = useParams()
    const { registerEvent, connectionReady } = useSignalRContext()
    const [selectedMissionDefinition, setSelectedMissionDefinition] = useState<CondensedMissionDefinition>()

    useEffect(() => {
        if (missionId) {
            BackendAPICaller.getMissionDefinitionById(missionId).then((mission) => {
                setSelectedMissionDefinition(mission)
            })
        }
    }, [missionId])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionDefinitionUpdated, (username: string, message: string) => {
                const missionDefinition: CondensedMissionDefinition = JSON.parse(message)
                missionDefinition.sourceType =
                    Object.values(SourceType)[missionDefinition.sourceType as unknown as number]
                if (missionDefinition.id === missionId) {
                    setSelectedMissionDefinition(missionDefinition)
                }
            })
        }
    }, [registerEvent, connectionReady, missionId])

    return (
        <>
            <Header page={'mission'} />
            {selectedMissionDefinition !== undefined && (
                <StyledDict.MissionDefinitionPage>
                    <BackButton />
                    <MissionDefinitionHeader missionDefinition={selectedMissionDefinition} />
                    <MissionDefinitionPageBody
                        missionDefinition={selectedMissionDefinition}
                        updateMissionDefinition={setSelectedMissionDefinition}
                    />
                </StyledDict.MissionDefinitionPage>
            )}
        </>
    )
}
