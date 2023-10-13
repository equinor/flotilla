import { ChangeEvent, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import styled from 'styled-components'
import { MissionDefinitionHeader } from './MissionDefinitionHeader/MissionDefinitionHeader'
import { BackButton } from '../../../utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { tokens } from '@equinor/eds-tokens'
import { Header } from 'components/Header/Header'
import { CondensedMissionDefinition, SourceType } from 'models/MissionDefinition'
import { Button, Typography, Card, Dialog, TextField, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { config } from 'config'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { Icons } from 'utils/icons'

const StyledFormCard = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
    gap: 25px;
`
const StyledButtonSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
`
const StyledFormContainer = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    margin: auto;
    gap: 10px;
`
const StyledFormItem = styled.div`
    width: 200px;
    padding: 5px;
    margin: auto;
`
const StyledDialog = styled(Dialog)`
    display: flex;
    justify-content: space-between;
    padding: 1rem;
    width: 620px;
`
const StyledMissionDefinitionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
`

function KeyValuePairDisplay({ left, right, onEdit }: { left: string; right: any, onEdit?: () => void }) {
    return (
        <>
            <Typography
                variant="body_long_bold"
                group="paragraph"
                color={tokens.colors.text.static_icons__secondary.hex}
            >
                {left}
                {onEdit && <Icon name={Icons.Edit} size={16} onClick={onEdit}></Icon>}
            </Typography>
            <Typography
                variant="body_long_italic"
                group="paragraph"
                color={tokens.colors.text.static_icons__secondary.hex}
            >
                {right}
            </Typography>
        </>
    )
}

interface IMissionDefinitionPageBodyProps {
    missionDefinition: CondensedMissionDefinition
    updateMissionDefinition: (missionDefinition: CondensedMissionDefinition) => void
}

function MissionDefinitionPageBody({ missionDefinition, updateMissionDefinition }: IMissionDefinitionPageBodyProps) {
    const { TranslateText } = useLanguageContext()
    let navigate = useNavigate()
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
    const [selectedField, setSelectedField] = useState<string>("")

    const displayInspectionFrequency = (inspectionFrequency: string | undefined | null) => {
        if (!inspectionFrequency) return TranslateText('No inspection frequency set')
        const timeArray = inspectionFrequency.split(':')
        const days: number = +timeArray[0]
        const hours: number = +timeArray[1]
        const minutes: number = +timeArray[2]
        let returnStringArray: string[] = []
        if (days > 0) returnStringArray.push(days + ' ' + TranslateText('days'))
        if (hours > 0) returnStringArray.push(hours + ' ' + TranslateText('hours'))
        if (minutes > 0) returnStringArray.push(minutes + ' ' + TranslateText('minutes'))
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
            <KeyValuePairDisplay left={TranslateText('Name')} right={missionDefinition.name} onEdit={onEdit('name')} />
            <KeyValuePairDisplay left={TranslateText('Comment')} right={missionDefinition.comment} onEdit={onEdit('comment')} />
            <KeyValuePairDisplay
                left={TranslateText('Inspection frequency')}
                right={displayInspectionFrequency(missionDefinition.inspectionFrequency)}
                onEdit={onEdit('inspectionFrequency')}
            />
            <KeyValuePairDisplay
                left={TranslateText('Area')}
                right={missionDefinition.area ? missionDefinition.area.areaName : '-'}
            />
            <KeyValuePairDisplay
                left={TranslateText('Deck')}
                right={missionDefinition.area ? missionDefinition.area.deckName : '-'}
            />
            <KeyValuePairDisplay
                left={TranslateText('Plant')}
                right={missionDefinition.area ? missionDefinition.area.plantCode : '-'}
            />
            <KeyValuePairDisplay
                left={TranslateText('Installation')}
                right={missionDefinition.area ? missionDefinition.area.installationCode : '-'}
            />
            <KeyValuePairDisplay
                left={TranslateText('Mission source')}
                right={TranslateText(missionDefinition.sourceType)}
            />
            <Button
                disabled={
                    missionDefinition.lastSuccessfulRun === undefined || missionDefinition.lastSuccessfulRun === null
                }
                onClick={() =>
                    navigate(`${config.FRONTEND_BASE_ROUTE}/mission/${missionDefinition.lastSuccessfulRun?.id}`)
                }
            >
                {TranslateText('View last run') +
                    (missionDefinition.lastSuccessfulRun ? '' : ': ' + TranslateText('Not yet performed'))}
            </Button>
            {
                isEditDialogOpen &&
                <MissionDefinitionEditDialog
                    fieldName={selectedField}
                    missionDefinition={missionDefinition}
                    closeEditDialog={() => setIsEditDialogOpen(false)}
                    updateMissionDefinition={updateMissionDefinition}
                />
            }
        </>
    )
}

interface IMissionDefinitionEditDialogProps {
    missionDefinition: CondensedMissionDefinition
    fieldName: string
    closeEditDialog: () => void
    updateMissionDefinition: (missionDefinition: CondensedMissionDefinition) => void
}

function MissionDefinitionEditDialog({ missionDefinition, updateMissionDefinition, fieldName, closeEditDialog }: IMissionDefinitionEditDialogProps) {
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
        newDay = newDay === '' ? '0' : newDay
        if (!form.inspectionFrequency) return newDay + '.00:00:00'
        const inspectionArray = form.inspectionFrequency.split(':')
        if (!inspectionArray || inspectionArray.length < 2) return newDay + '.00:00:00'
        setForm({ ...form, inspectionFrequency: newDay + '.00:' + inspectionArray[1] + ':00' })
    }

    const updateInspectionFrequencyFormHours = (newHour: string) => {
        if (!Number(newHour) && newHour !== '') return
        newHour = newHour === '' ? '0' : newHour
        if (!form.inspectionFrequency) return '00:' + newHour + ':00'
        const inspectionArray = form.inspectionFrequency.split(':')
        if (!inspectionArray || inspectionArray.length < 2) return '00:' + newHour + ':00'
        setForm({ ...form, inspectionFrequency: inspectionArray[0] + ':' + newHour + ':00' })
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
    const inspectionFrequencyHours =
        !inspectionFrequency[1] || inspectionFrequency[1] === 0 ? '' : String(inspectionFrequency[1])

    const getFormItem = () => {
        switch (fieldName) {
            case "inspectionFrequency":
                return <StyledFormItem>
                        <div>
                            <TextField
                                id="compact-textfield"
                                label={TranslateText('Days between inspections')}
                                unit={TranslateText('days')}
                                value={inspectionFrequencyDays}
                                onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                                    updateInspectionFrequencyFormDays(changes.target.value)
                                }
                            />
                            <TextField
                                id="compact-textfield"
                                label={TranslateText('Hours between inspections')}
                                unit={TranslateText('hours')}
                                value={inspectionFrequencyHours}
                                onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                                    updateInspectionFrequencyFormHours(changes.target.value)
                                }
                            />
                        </div>
                    </StyledFormItem>
            case "comment":
                return <StyledFormItem>
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
                    </StyledFormItem>
            case "name":
                return <StyledFormItem>
                        <TextField
                            id="nameEdit"
                            value={form.name ?? ''}
                            label={TranslateText("Name")}
                            onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                                setForm({ ...form, name: changes.target.value })
                            }
                        />
                    </StyledFormItem>
            default:
                console.error("Invalid field name: ", fieldName)
                break;
        }
    }

    return (
        <StyledDialog open={true}>
            <StyledFormCard>
                <Typography variant="h2">{TranslateText('Edit mission definition')}</Typography>
                <StyledFormContainer>
                    {getFormItem()}
                </StyledFormContainer>
                <StyledButtonSection>
                    <Button onClick={handleSubmit} variant="outlined" color="primary">
                        {' '}
                        {TranslateText('Update mission definition')}{' '}
                    </Button>
                    <Button onClick={() => closeEditDialog()} variant="outlined" color="primary">
                        {' '}
                        {TranslateText('Cancel')}{' '}
                    </Button>
                </StyledButtonSection>
            </StyledFormCard>
        </StyledDialog>
    )
}

export function MissionDefinitionPage() {
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
                <StyledMissionDefinitionPage>
                    <BackButton />
                    <MissionDefinitionHeader missionDefinition={selectedMissionDefinition} />
                    <MissionDefinitionPageBody
                        missionDefinition={selectedMissionDefinition}
                        updateMissionDefinition={setSelectedMissionDefinition}
                    />
                </StyledMissionDefinitionPage>
            )}
        </>
    )
}
