import { ChangeEvent, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import styled from 'styled-components'
import { MissionDefinitionHeader } from './MissionDefinitionHeader/MissionDefinitionHeader'
import { BackButton } from '../../../utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { tokens } from '@equinor/eds-tokens'
import { Header } from 'components/Header/Header'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { Button, Typography, Card, Dialog, TextField } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinitionUpdateForm } from 'models/MissionDefinitionUpdateForm'
import { config } from 'config'

const StyledFormDialog = styled.div`
    display: flex;
    justify-content: space-between;
`
const StyledAutoComplete = styled(Card)`
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
const StyledFormSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
`
const StyledDialog = styled(Dialog)`
    display: flex;
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

interface IProps {
    missionDefinition: CondensedMissionDefinition
    updateMissionDefinition: (missionDefinition: CondensedMissionDefinition) => void
}

function KeyValuePairDisplay({ left, right }: { left: string; right: any }) {
    return (
        <>
            <Typography
                variant="body_long_bold"
                group="paragraph"
                color={tokens.colors.text.static_icons__secondary.hex}
            >
                {left}
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

function MissionDefinitionPageBody({ missionDefinition, updateMissionDefinition }: IProps) {
    const { TranslateText } = useLanguageContext()
    let navigate = useNavigate()

    const displayInspectionFrequency = (inspectionFrequency: string) => {
        if (inspectionFrequency === null) return TranslateText('No inspection frequency set')
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

    return (
        <>
            <KeyValuePairDisplay left={TranslateText('Comment')} right={missionDefinition.comment} />
            <KeyValuePairDisplay
                left={TranslateText('Inspection frequency')}
                right={displayInspectionFrequency(missionDefinition.inspectionFrequency)}
            />
            <KeyValuePairDisplay left={TranslateText('Area')} right={missionDefinition.area.areaName} />
            <KeyValuePairDisplay left={TranslateText('Deck')} right={missionDefinition.area.deckName} />
            <KeyValuePairDisplay left={TranslateText('Plant')} right={missionDefinition.area.plantCode} />
            <KeyValuePairDisplay left={TranslateText('Installation')} right={missionDefinition.area.installationCode} />
            <KeyValuePairDisplay
                left={TranslateText('Mission source')}
                right={TranslateText(missionDefinition.sourceType)}
            />
            <Button
                disabled={missionDefinition.lastRun === null}
                onClick={() => navigate(`${config.FRONTEND_BASE_ROUTE}/mission/${missionDefinition.lastRun.id}`)}
            >
                {TranslateText('View last run') +
                    (missionDefinition.lastRun ? '' : ': ' + TranslateText('Not yet performed'))}
            </Button>
            <MissionDefinitionEditButtons
                missionDefinition={missionDefinition}
                updateMissionDefinition={updateMissionDefinition}
            />
        </>
    )
}

function MissionDefinitionEditButtons({ missionDefinition, updateMissionDefinition }: IProps) {
    const defaultMissionDefinitionForm: MissionDefinitionUpdateForm = {
        comment: missionDefinition.comment,
        inspectionFrequency: missionDefinition.inspectionFrequency,
        name: missionDefinition.name,
        isDeprecated: false,
    }
    const { TranslateText } = useLanguageContext()
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
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
            setIsEditDialogOpen(false)
            if (missionDefinition.isDeprecated) updateMissionDefinition(missionDefinition)
        })
    }

    const inspectionFrequency = getDayAndHoursFromInspectionFrequency(form.inspectionFrequency)
    const inspectionFrequencyDays =
        inspectionFrequency[0] === null || inspectionFrequency[0] === 0 ? '' : String(inspectionFrequency[0])
    const inspectionFrequencyHours =
        inspectionFrequency[1] === null || inspectionFrequency[1] === 0 ? '' : String(inspectionFrequency[1])

    return (
        <>
            <Button onClick={() => setIsEditDialogOpen(true)}>{TranslateText('Edit')}</Button>
            {isEditDialogOpen && (
                <StyledFormDialog>
                    <StyledDialog open={true}>
                        <StyledAutoComplete>
                            <StyledFormSection>
                                <TextField
                                    id="nameEdit"
                                    value={form.name ?? ''}
                                    label={TranslateText('Name')}
                                    onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                                        setForm({ ...form, name: changes.target.value })
                                    }
                                />
                                <TextField
                                    id="commentEdit"
                                    multiline
                                    rows={3}
                                    label={TranslateText('Comment')}
                                    value={form.comment ?? ''}
                                    onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                                        setForm({ ...form, comment: changes.target.value })
                                    }
                                />
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
                            </StyledFormSection>
                            <StyledButtonSection>
                                <Button onClick={handleSubmit} variant="outlined">
                                    {' '}
                                    {TranslateText('Update mission definition')}{' '}
                                </Button>
                                <Button onClick={() => setIsEditDialogOpen(false)} variant="outlined">
                                    {' '}
                                    {TranslateText('Cancel')}{' '}
                                </Button>
                            </StyledButtonSection>
                        </StyledAutoComplete>
                    </StyledDialog>
                </StyledFormDialog>
            )}
        </>
    )
}

export function MissionDefinitionPage() {
    const { missionId } = useParams()
    const [selectedMissionDefinition, setSelectedMissionDefinition] = useState<CondensedMissionDefinition>()

    useEffect(() => {
        if (missionId) {
            BackendAPICaller.getMissionDefinitionById(missionId).then((mission) => {
                setSelectedMissionDefinition(mission)
            })
        }
    }, [missionId])

    useEffect(() => {
        const timeDelay = 1000
        const id = setInterval(() => {
            if (missionId) {
                BackendAPICaller.getMissionDefinitionById(missionId).then((mission) => {
                    setSelectedMissionDefinition(mission)
                })
            }
        }, timeDelay)
        return () => clearInterval(id)
    }, [missionId])

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
