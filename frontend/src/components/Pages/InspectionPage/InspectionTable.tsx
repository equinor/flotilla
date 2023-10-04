import { Table, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Deck } from 'models/Deck'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { NavigateFunction, useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { Inspection, OngoingMissionType, compareInspections } from './InspectionSection'
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

const Circle = (fill: string) => (
    <svg xmlns="http://www.w3.org/2000/svg" width="13" height="14" viewBox="0 0 13 14" fill="none">
        <circle cx="6.5" cy="7" r="6.5" fill={fill} />
    </svg>
)

const RedCircle = Circle('#EB0000')
const YellowCircle = Circle('#FF9200')
const GreenCircle = Circle('#4BB748')

interface IProps {
    deck: Deck
    inspections: Inspection[]
    openDialog: () => void
    setSelectedMissions: (selectedMissions: CondensedMissionDefinition[]) => void
    ongoingMissions: OngoingMissionType
}

interface ITableProps {
    inspections: Inspection[]
    ongoingMissions: OngoingMissionType
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
                <>
                    {RedCircle} {TranslateTextWithContext('Past deadline')}
                </>
            )
        case deadlineDays > 0 && deadlineDays <= 1:
            return (
                <>
                    {RedCircle} {TranslateTextWithContext('Due today')}
                </>
            )
        case deadlineDays > 1 && deadlineDays <= 7:
            return (
                <>
                    {YellowCircle} {TranslateTextWithContext('Due this week')}
                </>
            )
        case deadlineDays > 7 && deadlineDays <= 14:
            return (
                <>
                    {YellowCircle} {TranslateTextWithContext('Due within two weeks')}
                </>
            )
        case deadlineDays > 7 && deadlineDays <= 30:
            return (
                <>
                    {GreenCircle} {TranslateTextWithContext('Due within a month')}
                </>
            )
    }
    return (
        <>
            {GreenCircle} {TranslateTextWithContext('Up to date')}
        </>
    )
}

const getInspectionRow = (
    inspection: Inspection,
    ongoingMissions: OngoingMissionType,
    openDialog: () => void,
    setScheduledMissions: (selectedMissions: CondensedMissionDefinition[]) => void,
    navigate: NavigateFunction
) => {
    const mission = inspection.missionDefinition
    let status
    let lastCompleted: string = ''
    const isScheduled = Object.keys(ongoingMissions).includes(mission.id) && ongoingMissions[mission.id]
    if (isScheduled) {
        status = (
            <>
                {GreenCircle} {TranslateTextWithContext('Already scheduled')}
            </>
        )
    } else {
        if (!mission.lastRun || !mission.lastRun.endTime) {
            if (inspection.missionDefinition.inspectionFrequency) {
                status = (
                    <>
                        {RedCircle} {TranslateTextWithContext('Not yet performed')}
                    </>
                )
            } else {
                status = TranslateTextWithContext('No planned inspection')
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

export function InspectionTable({ deck, inspections, openDialog, setSelectedMissions, ongoingMissions }: IProps) {
    const { TranslateText } = useLanguageContext()

    let navigate = useNavigate()

    let cellValues = inspections
        .sort(compareInspections)
        .map((inspection) => getInspectionRow(inspection, ongoingMissions, openDialog, setSelectedMissions, navigate))

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
    ongoingMissions,
    isDialogOpen,
    openDialog,
    closeDialog,
}: ITableProps) {
    const { TranslateText } = useLanguageContext()
    const [scheduledMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    let navigate = useNavigate()
    let cellValues = inspections
        .sort(compareInspections)
        .map((inspection) => getInspectionRow(inspection, ongoingMissions, openDialog, setSelectedMissions, navigate))

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
                    missions={scheduledMissions!}
                    refreshInterval={refreshInterval}
                    closeDialog={closeDialog}
                />
            )}
        </>
    )
}
