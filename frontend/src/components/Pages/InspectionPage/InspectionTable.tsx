import { Table, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Deck } from 'models/Deck'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { DeckMissionType, OngoingMissionType } from './InspectionSection'
import { getInspectionDeadline } from 'utils/StringFormatting'

const TableWithHeader = styled.div`
    gap: 2rem;
`

const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    height: 100%;
    width: 100%;
    scale: 50%;
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
    deckMissions: DeckMissionType
    openDialog: () => void
    setSelectedMissions: (selectedMissions: CondensedMissionDefinition[]) => void
    ongoingMissions: OngoingMissionType
}

export function InspectionTable({ deck, deckMissions, openDialog, setSelectedMissions, ongoingMissions }: IProps) {
    const { TranslateText } = useLanguageContext()
    
    let navigate = useNavigate()

    const formatDateString = (dateStr: Date) => {
        let newStr = dateStr.toString()
        newStr = newStr.slice(0, newStr.length - 8)
        newStr = newStr.replaceAll('T', ' ')
        return newStr
    }

    const getInspectionStatus = (inspectionFrequency: string, lastRunTime: Date) => {
        const deadlineDate = getInspectionDeadline(inspectionFrequency, lastRunTime)
        // The magical number on the right is the number of milliseconds in a day
        const deadline = new Date(deadlineDate.getTime() - new Date().getTime()).getTime() / 8.64e7

        if (deadline <= 0) {
            return (
                <>
                    {RedCircle} {TranslateText('Past deadline')}
                </>
            )
        } else if (deadline > 0 && deadline <= 1) {
            return (
                <>
                    {RedCircle} {TranslateText('Due today')}
                </>
            )
        } else if (deadline > 1 && deadline <= 7) {
            return (
                <>
                    {YellowCircle} {TranslateText('Due this week')}
                </>
            )
        } else if (deadline > 7 && deadline <= 14) {
            return (
                <>
                    {YellowCircle} {TranslateText('Due within two weeks')}
                </>
            )
        } else if (deadline > 7 && deadline <= 30) {
            return (
                <>
                    {GreenCircle} {TranslateText('Due within a month')}
                </>
            )
        }
        return (
            <>
                {GreenCircle} {TranslateText('Up to date')}
            </>
        )
    }

    const getInspectionRow = (mission: CondensedMissionDefinition) => {
        let status
        let lastCompleted: string = ''
        let deadline: string = ''
        const isScheduled = Object.keys(ongoingMissions).includes(mission.id) && ongoingMissions[mission.id]
        if (isScheduled) {
            status = (
                <>
                    {GreenCircle} {TranslateText('Already scheduled')}
                </>
            )
        } else {
            if (!mission.lastRun || !mission.lastRun.endTime) {
                status = (
                    <>
                        {RedCircle} {TranslateText('Not yet performed')}
                    </>
                )
                lastCompleted = TranslateText('Never')
            } else if (mission.inspectionFrequency) {
                status = getInspectionStatus(mission.inspectionFrequency, mission.lastRun.endTime!)
                lastCompleted = formatDateString(mission.lastRun.endTime!)
                deadline = getInspectionDeadline(mission.inspectionFrequency, mission.lastRun.endTime!).toDateString()
            } else {
                status = TranslateText('No planned inspection')
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
                <Table.Cell>{lastCompleted}</Table.Cell>
                <Table.Cell>{deadline}</Table.Cell>
                <Table.Cell>
                    <StyledIcon
                        color={`${tokens.colors.interactive.focus.hex}`}
                        name={Icons.AddOutlined}
                        size={16}
                        title={TranslateText('Add to queue')}
                        onClick={() => {
                            openDialog()
                            setSelectedMissions([mission])
                        }}
                    />
                </Table.Cell>
            </Table.Row>
        )
    }

    return (
        <TableWithHeader>
            <Typography variant="h3">
                {TranslateText('Inspection Missions') + ' ' + TranslateText('for') + ' ' + deck.deckName}
            </Typography>
            <Table>
                <Table.Head sticky>
                    <Table.Row>
                        <Table.Cell>{TranslateText('Status')}</Table.Cell>
                        <Table.Cell>{TranslateText('Name')}</Table.Cell>
                        <Table.Cell>{TranslateText('Description')}</Table.Cell>
                        <Table.Cell>{TranslateText('Last completed')}</Table.Cell>
                        <Table.Cell>{TranslateText('Deadline')}</Table.Cell>
                        <Table.Cell>{TranslateText('Add to queue')}</Table.Cell>
                    </Table.Row>
                </Table.Head>
                <Table.Body>
                    {deckMissions[deck.id].missionDefinitions.map((mission) => getInspectionRow(mission))}
                </Table.Body>
            </Table>
        </TableWithHeader>
    )
}