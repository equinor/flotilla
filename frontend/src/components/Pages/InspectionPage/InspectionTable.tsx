import { Table, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Deck } from 'models/Deck'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { NavigateFunction, useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { Inspection, ScheduledMissionType, compareInspections } from './InspectionSection'
import { getDeadlineInDays } from 'utils/StringFormatting'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { useState } from 'react'
import { refreshInterval } from '../FrontPage/FrontPage'
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
    display: flex;
    align-items: centre;
    gap: 5px;
`

const StyledCircle = styled.div`
    width: 13px;
    height: 13px;
    border-radius: 100px;
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
    isDialogOpen: boolean
    openDialog: () => void
    closeDialog: () => void
}

const formatDateString = (dateStr: Date) => {
    let newStr = dateStr.toString()
    newStr = newStr.slice(0, newStr.length - 8)
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
                    <StyledCircle style={{ background: 'orange' }} />
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

const getInspectionRow = (
    inspection: Inspection,
    scheduledMissions: ScheduledMissionType,
    ongoingMissions: ScheduledMissionType,
    openDialog: () => void,
    setScheduledMissions: (selectedMissions: CondensedMissionDefinition[]) => void,
    navigate: NavigateFunction
) => {
    const mission = inspection.missionDefinition
    let status
    let lastCompleted: string = ''
    const isScheduled = Object.keys(scheduledMissions).includes(mission.id) && scheduledMissions[mission.id]
    const isOngoing = Object.keys(ongoingMissions).includes(mission.id) && ongoingMissions[mission.id]

    if (isScheduled) {
        if (isOngoing) {
            status = (
                <StyledContent>
                    <Icon name={Icons.Ongoing} size={16} />
                    {TranslateTextWithContext('InProgress')}
                </StyledContent>
            )
        } else
            status = (
                <StyledContent>
                    <Icon name={Icons.Pending} size={16} />
                    {TranslateTextWithContext('Scheduled')}
                </StyledContent>
            )
    } else {
        if (!mission.lastRun || !mission.lastRun.endTime) {
            if (inspection.missionDefinition.inspectionFrequency) {
                status = (
                    <StyledContent>
                        <StyledCircle style={{ background: 'red' }} />
                        {TranslateTextWithContext('Not yet performed')}
                    </StyledContent>
                )
            } else {
                status = (
                    <StyledContent>
                        <StyledCircle style={{ background: 'green' }} />
                        {TranslateTextWithContext('No planned inspection')}
                    </StyledContent>
                )
            }
            lastCompleted = TranslateTextWithContext('Never')
        } else {
            status = inspection.missionDefinition.inspectionFrequency
                ? getInspectionStatus(inspection.deadline!)
                : TranslateTextWithContext('No planned inspection')
            lastCompleted = formatDateString(mission.lastRun.endTime!)
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
            <Table.Cell>{mission.area.areaName}</Table.Cell>
            <Table.Cell>{lastCompleted}</Table.Cell>
            <Table.Cell>{inspection.deadline ? inspection.deadline.toDateString() : ''}</Table.Cell>
            <Table.Cell>
                <StyledIcon
                    color={`${tokens.colors.interactive.focus.hex}`}
                    name={Icons.AddOutlined}
                    size={16}
                    title={TranslateTextWithContext('Add to queue')}
                    onClick={() => {
                        openDialog()
                        setScheduledMissions([mission])
                    }}
                />
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

    let navigate = useNavigate()

    let cellValues = inspections
        .sort(compareInspections)
        .map((inspection) =>
            getInspectionRow(inspection, scheduledMissions, ongoingMissions, openDialog, setSelectedMissions, navigate)
        )

    return (
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
                            <Table.Cell> {TranslateText(col)}</Table.Cell>
                        ))}
                    </Table.Row>
                </Table.Head>
                <Table.Body>{cellValues}</Table.Body>
            </Table>
        </StyledTable>
    )
}

export function AllInspectionsTable({
    inspections,
    scheduledMissions,
    ongoingMissions,
    isDialogOpen,
    openDialog,
    closeDialog,
}: ITableProps) {
    const { TranslateText } = useLanguageContext()
    const [scheduledSelectedMissions, setScheduledSelectedMissions] = useState<CondensedMissionDefinition[]>()
    let navigate = useNavigate()
    let cellValues = inspections
        .sort(compareInspections)
        .map((inspection) =>
            getInspectionRow(inspection, scheduledMissions, ongoingMissions, openDialog, setScheduledSelectedMissions, navigate)
        )

    return (
        <>
            <Table>
                <Table.Head sticky>
                    <Table.Row>
                        {columns.map((col) => (
                            <Table.Cell> {TranslateText(col)}</Table.Cell>
                        ))}
                    </Table.Row>
                </Table.Head>
                <Table.Body>{cellValues}</Table.Body>
            </Table>
            {isDialogOpen && (
                <ScheduleMissionDialog
                    missions={scheduledSelectedMissions!}
                    refreshInterval={refreshInterval}
                    closeDialog={closeDialog}
                />
            )}

        </>
    )
}
