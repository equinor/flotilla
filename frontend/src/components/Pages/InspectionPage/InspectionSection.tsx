import { Table, Card, Typography, Icon, Button, EdsProvider } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Deck } from 'models/Deck'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { RefreshProps } from '../FrontPage/FrontPage'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition, MissionDefinition } from 'models/MissionDefinition'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { Icons } from 'utils/icons'
import { MissionStatus } from 'models/Mission'

const StyledCard = styled(Card)`
    width: 250px;
    padding: 12px;
    border-radius: 20px;
    :hover {
        background-color: #deedee;
    }
`

const StyledCardComponent = styled.div`
    display: flex;
    flex-direction: row;
    padding-right: 10px;
    gap: 10px;
    width: 100%;
`

const StyledDeckCards = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
`

const TableWithHeader = styled.div`
    gap: 2rem;
`

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
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

interface DeckMissionType {
    [deckId: string]: { missionDefinitions: CondensedMissionDefinition[]; deck: Deck }
}

interface OngoingMissionType {
    [missionId: string]: boolean
}

const formatBackendDateTimeToDate = (date: Date) => {
    return new Date(date.toString())
}

const getInspectionDeadline = (inspectionFrequency: string, lastRunTime: Date): Date => {
    const dayHourSecondsArray = inspectionFrequency.split(':')
    const days: number = +dayHourSecondsArray[0]
    const hours: number = +dayHourSecondsArray[1]
    const minutes: number = +dayHourSecondsArray[2]

    lastRunTime = formatBackendDateTimeToDate(lastRunTime)

    let deadline = lastRunTime
    deadline.setDate(deadline.getDate() + days)
    deadline.setHours(deadline.getHours() + hours)
    deadline.setMinutes(deadline.getMinutes() + minutes)
    return deadline
    // More flexibly we can also define the deadline in terms of milliseconds:
    // new Date(lastRunTime.getTime() + (1000 * 60 * days) + (1000 * 60 * 60 * hours) + (1000 * 60 * 60 * 24 * days))
}

export function InspectionSection({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [deckMissions, setDeckMissions] = useState<DeckMissionType>({})
    const [ongoingMissions, setOngoingMissions] = useState<OngoingMissionType>({})
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setisDialogOpen] = useState<boolean>(false)
    let navigate = useNavigate()

    useEffect(() => {
        setSelectedDeck(undefined)
        BackendAPICaller.getDecks().then(async (decks: Deck[]) => {
            console.log(decks)
            let newDeckMissions: DeckMissionType = {}
            const filteredDecks = decks.filter(
                (deck) => deck.installationCode.toLowerCase() === installationCode.toLowerCase()
            )
            for (const deck of filteredDecks) {
                // These calls need to be made sequentially to update areaMissions safely
                let missionDefinitions = await BackendAPICaller.getMissionDefinitionsInDeck(deck)
                if (!missionDefinitions) missionDefinitions = []
                newDeckMissions[deck.id] = { missionDefinitions: missionDefinitions, deck: deck }
            }
            setDeckMissions(newDeckMissions)
        })
    }, [installationCode])

    const handleScheduleAll = (missions: CondensedMissionDefinition[]) => {
        setisDialogOpen(true)
        missions.sort((m1, m2) => {
            if (!m1.lastRun) return 1
            else if (!m2.lastRun) return -1
            else return getInspectionDeadline(m1.inspectionFrequency, m1.lastRun.endTime!).getTime() 
                - getInspectionDeadline(m2.inspectionFrequency, m2.lastRun.endTime!).getTime()
        })
        setSelectedMissions(missions)
    }

    const updateOngoingMissionsMap = async (areaMissions: DeckMissionType) => {
        let newOngoingMissions: OngoingMissionType = {}
        for (const areaId of Object.keys(areaMissions)) {
            for (const missionDefinition of areaMissions[areaId].missionDefinitions) {
                const missionRuns = await BackendAPICaller.getMissionRuns({
                    statuses: [MissionStatus.Paused, MissionStatus.Pending, MissionStatus.Ongoing],
                    missionId: missionDefinition.id,
                })
                newOngoingMissions[missionDefinition.id] = missionRuns.content.length > 0
            }
        }
        setOngoingMissions(newOngoingMissions)
    }

    useEffect(() => {
        const id = setInterval(() => {
            updateOngoingMissionsMap(deckMissions)
        }, refreshInterval)
        return () => clearInterval(id)
    }, [deckMissions])

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

    const formatDateString = (dateStr: Date) => {
        let newStr = dateStr.toString()
        newStr = newStr.slice(0, newStr.length - 8)
        newStr = newStr.replaceAll('T', ' ')
        return newStr
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
                            setisDialogOpen(true)
                            setSelectedMissions([mission])
                        }}
                    />
                </Table.Cell>
            </Table.Row>
        )
    }

    const getInspectionsTable = (deck: Deck) => (
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

    return (
        <>
            <Typography variant="h1">{TranslateText('Deck Inspections')}</Typography>
            <StyledContent>
                <StyledDeckCards>
                    {Object.keys(deckMissions).length > 0 ? (
                        Object.keys(deckMissions).map((deckId) => (
                            <StyledCard
                                variant="default"
                                key={deckId}
                                style={{ boxShadow: tokens.elevation.raised }}
                                onClick={() => setSelectedDeck(deckMissions[deckId].deck)}
                            >
                                <Typography>{deckMissions[deckId].deck.deckName.toLocaleUpperCase()}</Typography>
                                <StyledCardComponent>
                                    <Typography>
                                        {deckMissions[deckId] &&
                                            deckMissions[deckId].missionDefinitions.length > 0 &&
                                            deckMissions[deckId].missionDefinitions.length + ' ' + TranslateText('Missions')}
                                    </Typography>
                                    <EdsProvider density='compact'>
                                        <Button onClick={() => handleScheduleAll(deckMissions[deckId].missionDefinitions)}>{TranslateText('Schedule all')}</Button>
                                    </EdsProvider>
                                </StyledCardComponent>
                            </StyledCard>
                        ))
                    ) : (
                        <Typography variant="h1">{TranslateText('No Deck Inspections Available')}</Typography>
                    )}
                </StyledDeckCards>
                {selectedDeck && getInspectionsTable(selectedDeck)}
            </StyledContent>
            {isDialogOpen && (
                <ScheduleMissionDialog
                    missions={selectedMissions!}
                    refreshInterval={refreshInterval}
                    closeDialog={() => setisDialogOpen(false)}
                />
            )}
        </>
    )
}
