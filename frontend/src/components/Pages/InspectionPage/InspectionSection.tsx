import { Card, Typography, Icon, Button, EdsProvider } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Deck } from 'models/Deck'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { RefreshProps } from '../FrontPage/FrontPage'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { useNavigate } from 'react-router-dom'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { MissionStatus } from 'models/Mission'
import { getDeadlineInDays, getInspectionDeadline } from 'utils/StringFormatting'
import { InspectionTable } from './InspectionTable'

const StyledCard = styled(Card)`
    width: 280px;
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

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`

export interface Inspection {
    missionDefinition: CondensedMissionDefinition
    deadline: Date | undefined
}

export interface DeckMissionType {
    [deckId: string]: {
        inspections: Inspection[]
        deck: Deck
    }
}

export interface OngoingMissionType {
    [missionId: string]: boolean
}

export const compareInspections = (i1: Inspection, i2: Inspection) => {
    if (!i1.missionDefinition.inspectionFrequency) return 1
    if (!i2.missionDefinition.inspectionFrequency) return -1
    if (!i1.missionDefinition.lastRun) return 1
    if (!i2.missionDefinition.lastRun) return -1
    else return i1.deadline!.getTime() - i2.deadline!.getTime()
}

export function InspectionSection({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [deckMissions, setDeckMissions] = useState<DeckMissionType>({})
    const [ongoingMissions, setOngoingMissions] = useState<OngoingMissionType>({})
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setisDialogOpen] = useState<boolean>(false)

    useEffect(() => {
        setSelectedDeck(undefined)
        BackendAPICaller.getDecks().then(async (decks: Deck[]) => {
            let newDeckMissions: DeckMissionType = {}
            const filteredDecks = decks.filter(
                (deck) => deck.installationCode.toLowerCase() === installationCode.toLowerCase()
            )
            for (const deck of filteredDecks) {
                // These calls need to be made sequentially to update areaMissions safely
                let missionDefinitions = await BackendAPICaller.getMissionDefinitionsInDeck(deck)
                if (!missionDefinitions) missionDefinitions = []
                newDeckMissions[deck.id] = {
                    inspections: missionDefinitions.map((m) => {
                        return {
                            missionDefinition: m,
                            deadline: m.lastRun
                                ? getInspectionDeadline(m.inspectionFrequency, m.lastRun.endTime!)
                                : undefined,
                        }
                    }),
                    deck: deck,
                }
            }
            setDeckMissions(newDeckMissions)
        })
    }, [installationCode])

    const handleScheduleAll = (inspections: Inspection[]) => {
        setisDialogOpen(true)
        inspections.sort(compareInspections)
        setSelectedMissions(inspections.map((i) => i.missionDefinition))
    }

    const updateOngoingMissionsMap = async (areaMissions: DeckMissionType) => {
        let newOngoingMissions: OngoingMissionType = {}
        for (const areaId of Object.keys(areaMissions)) {
            for (const inspection of areaMissions[areaId].inspections) {
                const missionDefinition = inspection.missionDefinition
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

    const getCardColor = (deckId: string) => {
        const inspections = deckMissions[deckId].inspections
        if (inspections.length === 0) return 'gray'
        const isScheduled = (m: CondensedMissionDefinition) =>
            Object.keys(ongoingMissions).includes(m.id) && ongoingMissions[m.id]
        const sortedInspections = inspections.filter((i) => !isScheduled(i.missionDefinition)).sort(compareInspections)
        if (sortedInspections.length === 0) return 'green'

        const nextInspection = sortedInspections[0]
        if (!nextInspection.deadline) return 'green'

        const deadlineDays = getDeadlineInDays(nextInspection.deadline)
        switch (true) {
            case deadlineDays <= 1:
                return 'red'
            case deadlineDays > 1 && deadlineDays <= 14:
                return 'yellow'
            case deadlineDays > 7:
                return 'green'
        }
    }

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
                                style={{
                                    boxShadow: tokens.elevation.raised,
                                    borderStyle: 'solid',
                                    borderWidth: '3px',
                                    borderColor: `${getCardColor(deckId)}`,
                                }}
                                onClick={() => setSelectedDeck(deckMissions[deckId].deck)}
                            >
                                <Typography>{deckMissions[deckId].deck.deckName.toLocaleUpperCase()}</Typography>
                                <StyledCardComponent>
                                    <Typography>
                                        {deckMissions[deckId] &&
                                            deckMissions[deckId].inspections.length > 0 &&
                                            deckMissions[deckId].inspections.length + ' ' + TranslateText('Missions')}
                                    </Typography>
                                    <EdsProvider density="compact">
                                        <Button onClick={() => handleScheduleAll(deckMissions[deckId].inspections)}>
                                            {TranslateText('Schedule all')}
                                        </Button>
                                    </EdsProvider>
                                </StyledCardComponent>
                            </StyledCard>
                        ))
                    ) : (
                        <Typography variant="h1">{TranslateText('No Deck Inspections Available')}</Typography>
                    )}
                </StyledDeckCards>
                {selectedDeck && (
                    <InspectionTable
                        deck={selectedDeck}
                        openDialog={() => setisDialogOpen(true)}
                        setSelectedMissions={setSelectedMissions}
                        inspections={deckMissions[selectedDeck.id].inspections}
                        ongoingMissions={ongoingMissions}
                    />
                )}
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
