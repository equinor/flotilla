import { Table, Typography, Icon, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Deck } from 'models/Deck'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { NavigateFunction, useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { Inspection } from './InspectionSection'
import { compareInspections } from './InspectionUtilities'
import { getDeadlineInDays } from 'utils/StringFormatting'
import { AlreadyScheduledMissionDialog, ScheduleMissionDialog } from './ScheduleMissionDialogs'
import { useEffect, useState } from 'react'
import { TranslateTextWithContext } from 'components/Contexts/LanguageContext'
import { useMissionsContext } from 'components/Contexts/MissionListsContext'

const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    height: 100%;
    width: 100%;
    scale: 50%;
`

const StyledTable = styled.div`
    display: grid;
    overflow: auto;
    @media (max-width: 700px) {
        width: calc(100vw - 30px);
    }
    max-width: 950px;
`

const StyledContent = styled.div`
    display: grid;
    grid-template-columns: 14px auto;
    align-items: center;
    gap: 4px;
`

const StyledCircle = styled.div`
    width: 13px;
    height: 13px;
    border-radius: 50px;
`

interface IProps {
    deck: Deck
    inspections: Inspection[]
    openDialog: () => void
    setSelectedMissions: (selectedMissions: CondensedMissionDefinition[]) => void
}

interface ITableProps {
    inspections: Inspection[]
}

const formatDateString = (dateStr: Date | string) => {
    let newStr = dateStr.toString()
    newStr = newStr.slice(0, 19)
    newStr = newStr.replaceAll('T', ' ')
    return newStr
}

const getStatusColorAndTextFromDeadline = (deadlineDate: Date): { statusColor: string; statusText: string } => {
    const deadlineDays = getDeadlineInDays(deadlineDate)

    switch (true) {
        case deadlineDays <= 0:
            return { statusColor: 'red', statusText: 'Past deadline' }
        case deadlineDays > 0 && deadlineDays <= 1:
            return { statusColor: 'red', statusText: 'Due today' }
        case deadlineDays > 1 && deadlineDays <= 7:
            return { statusColor: 'red', statusText: 'Due this week' }
        case deadlineDays > 7 && deadlineDays <= 14:
            return { statusColor: 'orange', statusText: 'Due within two weeks' }
        case deadlineDays > 7 && deadlineDays <= 30:
            return { statusColor: 'green', statusText: 'Due within a month' }
    }
    return { statusColor: 'green', statusText: 'Up to date' }
}

interface IInspectionRowProps {
    inspection: Inspection
    openDialog: () => void
    setMissions: (selectedMissions: CondensedMissionDefinition[]) => void
    openScheduledDialog: () => void
    navigate: NavigateFunction
}

const InspectionRow = ({ inspection, openDialog, setMissions, openScheduledDialog, navigate }: IInspectionRowProps) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const mission = inspection.missionDefinition
    let status
    let lastCompleted: string = ''
    const isScheduled = missionQueue.map((m) => m.missionId).includes(mission.id)
    const isOngoing = ongoingMissions.map((m) => m.missionId).includes(mission.id)

    if (isOngoing) {
        status = (
            <StyledContent>
                <Icon name={Icons.Ongoing} size={16} />
                {TranslateText('InProgress')}
            </StyledContent>
        )
    } else if (isScheduled) {
        status = (
            <StyledContent>
                <Icon name={Icons.Pending} size={16} />
                {TranslateText('Pending')}
            </StyledContent>
        )
    } else if (!mission.lastSuccessfulRun || !mission.lastSuccessfulRun.endTime) {
        if (inspection.missionDefinition.inspectionFrequency) {
            status = (
                <StyledContent>
                    <StyledCircle style={{ background: 'red' }} />
                    {TranslateText('Not yet performed')}
                </StyledContent>
            )
        } else {
            status = (
                <StyledContent>
                    <StyledCircle style={{ background: 'green' }} />
                    {TranslateText('No planned inspection')}
                </StyledContent>
            )
        }
        lastCompleted = TranslateText('Never')
    } else {
        const { statusColor, statusText } = inspection.missionDefinition.inspectionFrequency
            ? getStatusColorAndTextFromDeadline(inspection.deadline!)
            : { statusColor: 'green', statusText: 'No planned inspection' }
        status = (
            <StyledContent>
                <StyledCircle style={{ background: statusColor }} />
                {TranslateText(statusText)}
            </StyledContent>
        )
        lastCompleted = formatDateString(mission.lastSuccessfulRun.endTime!)
    }

    return (
        <Table.Row key={mission.id}>
            <Table.Cell>{status}</Table.Cell>
            <Table.Cell>
                <Typography
                    link
                    onClick={() => navigate(`${config.FRONTEND_BASE_ROUTE}/mission-definition/${mission.id}`)}
                >
                    {mission.name}
                </Typography>
            </Table.Cell>
            <Table.Cell>{mission.comment}</Table.Cell>
            <Table.Cell>{mission.area ? mission.area.areaName : '-'}</Table.Cell>
            <Table.Cell>{lastCompleted}</Table.Cell>
            <Table.Cell>{inspection.deadline ? formatDateString(inspection.deadline.toISOString()) : ''}</Table.Cell>
            <Table.Cell>
                {!isScheduled && (
                    <Button
                        variant="ghost_icon"
                        onClick={() => {
                            openDialog()
                            setMissions([mission])
                        }}
                    >
                        <StyledIcon
                            color={`${tokens.colors.interactive.focus.rgba}`}
                            name={Icons.AddOutlined}
                            size={24}
                            title={TranslateTextWithContext('Add to queue')}
                        />
                    </Button>
                )}
                {isScheduled && (
                    <Button
                        variant="ghost_icon"
                        onClick={() => {
                            openScheduledDialog()
                            setMissions([mission])
                        }}
                    >
                        <StyledIcon
                            color={`${tokens.colors.interactive.focus.hex}`}
                            name={Icons.AddOutlined}
                            size={24}
                            title={TranslateTextWithContext('Add to queue')}
                        />
                    </Button>
                )}
            </Table.Cell>
        </Table.Row>
    )
}

const columns = ['Status', 'Name', 'Description', 'Area', 'Last completed', 'Deadline', 'Add to queue']

export const InspectionTable = ({ deck, inspections, openDialog, setSelectedMissions }: IProps) => {
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const [isScheduledDialogOpen, setIsScheduledDialogOpen] = useState<boolean>(false)

    const openScheduleDialog = () => {
        setIsScheduledDialogOpen(true)
    }

    const closeScheduleDialog = () => {
        setIsScheduledDialogOpen(false)
    }

    const cellValues = inspections
        .sort(compareInspections)
        .map((inspection) => (
            <InspectionRow
                key={inspection.missionDefinition.id}
                inspection={inspection}
                openDialog={openDialog}
                setMissions={setSelectedMissions}
                openScheduledDialog={openScheduleDialog}
                navigate={navigate}
            />
        ))

    return (
        <>
            <StyledTable>
                <Table>
                    <Table.Caption>
                        <Typography variant="h3" style={{ marginBottom: '14px' }}>
                            {TranslateText('Inspection Missions') + ' ' + TranslateText('for') + ' ' + deck.deckName}
                        </Typography>
                    </Table.Caption>
                    <Table.Head sticky>
                        <Table.Row>
                            {columns.map((col) => (
                                <Table.Cell key={col}>{TranslateText(col)}</Table.Cell>
                            ))}
                        </Table.Row>
                    </Table.Head>
                    <Table.Body>{cellValues}</Table.Body>
                </Table>
            </StyledTable>
            {isScheduledDialogOpen && (
                <AlreadyScheduledMissionDialog openDialog={openDialog} closeDialog={closeScheduleDialog} />
            )}
        </>
    )
}

export const AllInspectionsTable = ({ inspections }: ITableProps) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isScheduledDialogOpen, setIsScheduledDialogOpen] = useState<boolean>(false)
    const [unscheduledMissions, setUnscheduledMissions] = useState<CondensedMissionDefinition[]>([])
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)

    const openDialog = () => {
        setIsDialogOpen(true)
    }

    const openScheduleDialog = () => {
        setIsScheduledDialogOpen(true)
    }

    const closeDialog = () => {
        setIsAlreadyScheduled(false)
        setSelectedMissions([])
        setUnscheduledMissions([])
        setIsDialogOpen(false)
    }

    const closeScheduleDialog = () => {
        setIsScheduledDialogOpen(false)
    }

    useEffect(() => {
        const isScheduled = (mission: CondensedMissionDefinition) =>
            missionQueue.map((m) => m.missionId).includes(mission.id)
        const isOngoing = (mission: CondensedMissionDefinition) =>
            ongoingMissions.map((m) => m.missionId).includes(mission.id)
        let unscheduledMissions: CondensedMissionDefinition[] = []
        if (selectedMissions) {
            selectedMissions.forEach((mission) => {
                if (isOngoing(mission) || isScheduled(mission)) setIsAlreadyScheduled(true)
                else unscheduledMissions = unscheduledMissions.concat([mission])
            })
            setUnscheduledMissions(unscheduledMissions)
        }
    }, [isDialogOpen, ongoingMissions, missionQueue, selectedMissions])

    const navigate = useNavigate()
    const cellValues = inspections
        .sort(compareInspections)
        .map((inspection) => (
            <InspectionRow
                key={inspection.missionDefinition.id}
                inspection={inspection}
                openDialog={openDialog}
                setMissions={setSelectedMissions}
                openScheduledDialog={openScheduleDialog}
                navigate={navigate}
            />
        ))

    return (
        <>
            <StyledTable>
                <Table>
                    <Table.Head sticky>
                        <Table.Row>
                            {columns.map((col) => (
                                <Table.Cell key={col}>{TranslateText(col)}</Table.Cell>
                            ))}
                        </Table.Row>
                    </Table.Head>
                    <Table.Body>{cellValues}</Table.Body>
                </Table>
                {isDialogOpen && (
                    <ScheduleMissionDialog
                        missions={selectedMissions!}
                        closeDialog={closeDialog}
                        setMissions={setSelectedMissions}
                        unscheduledMissions={unscheduledMissions}
                        isAlreadyScheduled={isAlreadyScheduled}
                    />
                )}
                {isScheduledDialogOpen && (
                    <AlreadyScheduledMissionDialog openDialog={openDialog} closeDialog={closeScheduleDialog} />
                )}
            </StyledTable>
        </>
    )
}
