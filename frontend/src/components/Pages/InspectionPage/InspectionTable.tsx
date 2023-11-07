import { Table, Typography, Icon, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Deck } from 'models/Deck'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { NavigateFunction, useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { Inspection, ScheduledMissionType } from './InspectionSection'
import { compareInspections } from './InspectionUtilities'
import { getDeadlineInDays } from 'utils/StringFormatting'
import { AlreadyScheduledMissionDialog, ScheduleMissionDialog } from './ScheduleMissionDialogs'
import { useEffect, useState } from 'react'
import { TranslateTextWithContext } from 'components/Contexts/LanguageContext'

const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    height: 100%;
    width: 100%;
    scale: 50%;
`

const StyledTable = styled.div`
    display: flex;
    align-items: flex-start;
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
    scheduledMissions: ScheduledMissionType
    ongoingMissions: ScheduledMissionType
}

interface ITableProps {
    inspections: Inspection[]
    scheduledMissions: ScheduledMissionType
    ongoingMissions: ScheduledMissionType
}

const formatDateString = (dateStr: Date | string) => {
    let newStr = dateStr.toString()
    newStr = newStr.slice(0, 19)
    newStr = newStr.replaceAll('T', ' ')
    return newStr
}

export const getInspectionStatus = (deadlineDate: Date) => {
    const deadlineDays = getDeadlineInDays(deadlineDate)

    switch (true) {
        case deadlineDays <= 0:
            return (
                <StyledContent>
                    <StyledCircle style={{ background: 'red' }} />
                    {TranslateTextWithContext('Past deadline')}
                </StyledContent>
            )
        case deadlineDays > 0 && deadlineDays <= 1:
            return (
                <StyledContent>
                    <StyledCircle style={{ background: 'red' }} /> {TranslateTextWithContext('Due today')}
                </StyledContent>
            )
        case deadlineDays > 1 && deadlineDays <= 7:
            return (
                <StyledContent>
                    <StyledCircle style={{ background: 'red' }} />
                    {TranslateTextWithContext('Due this week')}
                </StyledContent>
            )
        case deadlineDays > 7 && deadlineDays <= 14:
            return (
                <StyledContent>
                    <StyledCircle style={{ background: 'orange' }} /> {TranslateTextWithContext('Due within two weeks')}
                </StyledContent>
            )
        case deadlineDays > 7 && deadlineDays <= 30:
            return (
                <StyledContent>
                    <StyledCircle style={{ background: 'green' }} />
                    {TranslateTextWithContext('Due within a month')}
                </StyledContent>
            )
    }
    return (
        <StyledContent>
            <StyledCircle style={{ background: 'green' }} />
            {TranslateTextWithContext('Up to date')}
        </StyledContent>
    )
}

interface IInspectionRowProps {
    inspection: Inspection
    scheduledMissions: ScheduledMissionType
    ongoingMissions: ScheduledMissionType
    openDialog: () => void
    setMissions: (selectedMissions: CondensedMissionDefinition[]) => void
    openScheduledDialog: () => void
    navigate: NavigateFunction
}

const InspectionRow = ({
    inspection,
    scheduledMissions,
    ongoingMissions,
    openDialog,
    setMissions,
    openScheduledDialog,
    navigate,
}: IInspectionRowProps) => {
    const { TranslateText } = useLanguageContext()
    const mission = inspection.missionDefinition
    let status
    let lastCompleted: string = ''
    const isScheduled = Object.keys(scheduledMissions).includes(mission.id) && scheduledMissions[mission.id]
    const isOngoing = Object.keys(ongoingMissions).includes(mission.id) && ongoingMissions[mission.id]

    if (isScheduled || isOngoing) {
        if (isOngoing) {
            status = (
                <StyledContent>
                    <Icon name={Icons.Ongoing} size={16} />
                    {TranslateText('InProgress')}
                </StyledContent>
            )
        } else
            status = (
                <StyledContent>
                    <Icon name={Icons.Pending} size={16} />
                    {TranslateText('Pending')}
                </StyledContent>
            )
    } else {
        if (!mission.lastSuccessfulRun || !mission.lastSuccessfulRun.endTime) {
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
            status = inspection.missionDefinition.inspectionFrequency ? (
                getInspectionStatus(inspection.deadline!)
            ) : (
                <StyledContent>
                    <StyledCircle style={{ background: 'green' }} />
                    {TranslateText('No planned inspection')}
                </StyledContent>
            )
            lastCompleted = formatDateString(mission.lastSuccessfulRun.endTime!)
        }
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

export function InspectionTable({
    deck,
    inspections,
    openDialog,
    setSelectedMissions,
    scheduledMissions,
    ongoingMissions,
}: IProps) {
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
                scheduledMissions={scheduledMissions}
                ongoingMissions={ongoingMissions}
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

export function AllInspectionsTable({ inspections, scheduledMissions, ongoingMissions }: ITableProps) {
    const { TranslateText } = useLanguageContext()
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
        let unscheduledMissions: CondensedMissionDefinition[] = []
        if (selectedMissions) {
            selectedMissions.forEach((mission) => {
                if (Object.keys(scheduledMissions).includes(mission.id) && scheduledMissions[mission.id])
                    setIsAlreadyScheduled(true)
                else unscheduledMissions = unscheduledMissions.concat([mission])
            })
            setUnscheduledMissions(unscheduledMissions)
        }
    }, [isDialogOpen, scheduledMissions, selectedMissions])

    const navigate = useNavigate()
    const cellValues = inspections
        .sort(compareInspections)
        .map((inspection) => (
            <InspectionRow
                key={inspection.missionDefinition.id}
                inspection={inspection}
                scheduledMissions={scheduledMissions}
                ongoingMissions={ongoingMissions}
                openDialog={openDialog}
                setMissions={setSelectedMissions}
                openScheduledDialog={openScheduleDialog}
                navigate={navigate}
            />
        ))

    return (
        <>
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
        </>
    )
}
