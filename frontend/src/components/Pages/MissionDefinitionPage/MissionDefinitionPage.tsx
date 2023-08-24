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
    width: 320px;
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
                color={tokens.colors.text.static_icons__secondary.rgba}
            >
                {left}
            </Typography>
            <Typography
                variant="body_long_italic"
                group="paragraph"
                color={tokens.colors.text.static_icons__secondary.rgba}
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
    const defaultMissionDefinitionForm = {
        comment: missionDefinition.comment,
        inspectionfrequency: missionDefinition.inspectionFrequency,
        name: missionDefinition.name,
        isDeprecated: false,
    }
    const { TranslateText } = useLanguageContext()
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState<boolean>(false)
    const [form, setForm] = useState<MissionDefinitionUpdateForm>(defaultMissionDefinitionForm)

    const handleSubmit = () => {
        BackendAPICaller.updateMissionDefinition(missionDefinition.id, form).then((missionDefinition) => {
            setIsEditDialogOpen(false)
            if (missionDefinition.isDeprecated) updateMissionDefinition(missionDefinition)
        })
    }

    const handleDelete = () => {
        BackendAPICaller.deleteMissionDefinition(missionDefinition.id).then(() => {
            setIsEditDialogOpen(false)
            window.location.href = `${config.FRONTEND_URL}/`
        })
    }

    // TODO: need to set inspection frequency. Use two number selectors, days and hours

    return (
        <>
            <Button onClick={() => setIsEditDialogOpen(true)}>{TranslateText('Edit')}</Button>
            <Button onClick={() => setIsDeleteDialogOpen(true)}>{TranslateText('Delete')}</Button>
            {isEditDialogOpen && (
                <StyledFormDialog>
                    <StyledDialog open={true}>
                        <StyledAutoComplete>
                            <StyledFormSection>
                                <TextField
                                    id="commentEdit"
                                    multiline
                                    rows={3}
                                    value={form.comment}
                                    onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                                        setForm({ ...form, comment: changes.target.value })
                                    }
                                />
                                <TextField
                                    id="nameEdit"
                                    value={form.name}
                                    onChange={(changes: ChangeEvent<HTMLInputElement>) =>
                                        setForm({ ...form, name: changes.target.value })
                                    }
                                />
                            </StyledFormSection>
                            <StyledButtonSection>
                                <Button onClick={handleSubmit} variant="outlined" color="primary">
                                    {' '}
                                    {TranslateText('Update mission definition')}{' '}
                                </Button>
                                <Button onClick={() => setIsEditDialogOpen(false)} variant="outlined" color="primary">
                                    {' '}
                                    {TranslateText('Cancel')}{' '}
                                </Button>
                            </StyledButtonSection>
                        </StyledAutoComplete>
                    </StyledDialog>
                </StyledFormDialog>
            )}

            {/*isDeleteDialogOpen &&
                <StyledFormDialog>
                    <StyledDialog open={true}>
                        <StyledAutoComplete>
                            <Typography variant="caption">{TranslateText('Are you sure you want to delete') + ' ' + missionDefinition.name + '?'}</Typography>
                            <StyledButtonSection>
                                <Button
                                    onClick={handleDelete}
                                    variant="outlined"
                                    color="primary"
                                >
                                    {' '}
                                    {TranslateText('Yes')}{' '}
                                </Button>
                                <Button
                                    onClick={() => setIsEditDialogOpen(false)}
                                    variant="outlined"
                                    color="primary"
                                >
                                    {' '}
                                    {TranslateText('Cancel')}{' '}
                                </Button>
                            </StyledButtonSection>
                        </StyledAutoComplete>
                    </StyledDialog>
                </StyledFormDialog>*/}
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
