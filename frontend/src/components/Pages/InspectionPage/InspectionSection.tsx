import { useState, useEffect, useMemo } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Deck } from 'models/Deck'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { ScheduleMissionDialog } from './ScheduleMissionDialogs'
import { getInspectionDeadline } from 'utils/StringFormatting'
import { InspectionTable } from './InspectionTable'
import { StyledDict, compareInspections } from './InspectionUtilities'
import { DeckCards } from './DeckCards'
import { Area } from 'models/Area'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'

export interface Inspection {
    missionDefinition: CondensedMissionDefinition
    deadline: Date | undefined
}

export interface DeckInspectionTuple {
    areas: Area[]
    inspections: Inspection[]
    deck: Deck
}

interface DeckAreaTuple {
    areas: Area[]
    deck: Deck
}

export const InspectionSection = () => {
    const { installationCode } = useInstallationContext()
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [decks, setDecks] = useState<DeckAreaTuple[]>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const { missionDefinitions } = useMissionDefinitionsContext()

    const closeDialog = () => {
        setIsAlreadyScheduled(false)
        setSelectedMissions([])
        setIsDialogOpen(false)
    }

    const isScheduled = (mission: CondensedMissionDefinition) =>
        missionQueue.map((m) => m.missionId).includes(mission.id)
    const isOngoing = (mission: CondensedMissionDefinition) =>
        ongoingMissions.map((m) => m.missionId).includes(mission.id)

    const unscheduledMissions = selectedMissions?.filter((m) => !isOngoing(m) && !isScheduled(m))

    useEffect(() => {
        if (selectedMissions && selectedMissions.some((mission) => isOngoing(mission) || isScheduled(mission)))
            setIsAlreadyScheduled(true)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [ongoingMissions, missionQueue, selectedMissions])

    useMemo(() => {
        setSelectedDeck(undefined)

        // Fetch relevant decks and their areas from the database
        BackendAPICaller.getDecks().then(async (decks: Deck[]) =>
            setDecks(
                await Promise.all(
                    // This is needed since the map function uses async calls
                    decks
                        .filter((deck) => deck.installationCode.toLowerCase() === installationCode.toLowerCase())
                        .map(async (deck) => {
                            return {
                                areas: await BackendAPICaller.getAreasByDeckId(deck.id),
                                deck: deck
                            }
                        })
                )
            )
        )
    }, [installationCode])

    const deckMissions: DeckInspectionTuple[] = decks?.map(({ areas, deck }) => {
        const missionDefinitionsInDeck = missionDefinitions.filter((m) => m.area?.deckName === deck.deckName)
        return {
            inspections: missionDefinitionsInDeck.map((m) => {
                return {
                    missionDefinition: m,
                    deadline: m.lastSuccessfulRun
                        ? getInspectionDeadline(m.inspectionFrequency, m.lastSuccessfulRun.endTime!)
                        : undefined,
                }
            }),
            areas: areas,
            deck: deck,
        }
    }) ?? []

    const handleScheduleAll = (inspections: Inspection[]) => {
        setIsDialogOpen(true)
        const sortedInspections = inspections.sort(compareInspections)
        setSelectedMissions(sortedInspections.map((i) => i.missionDefinition))
    }

    return (
        <>
            <StyledDict.DeckOverview>
                <DeckCards
                    deckMissions={deckMissions}
                    setSelectedDeck={setSelectedDeck}
                    selectedDeck={selectedDeck}
                    handleScheduleAll={handleScheduleAll}
                />
                {selectedDeck && (
                    <InspectionTable
                        deck={selectedDeck}
                        openDialog={() => setIsDialogOpen(true)}
                        setSelectedMissions={setSelectedMissions}
                        inspections={deckMissions.find((d) => d.deck.deckName === selectedDeck.deckName)!.inspections}
                    />
                )}
            </StyledDict.DeckOverview>
            {isDialogOpen && (
                <ScheduleMissionDialog
                    missions={selectedMissions!}
                    closeDialog={closeDialog}
                    setMissions={setSelectedMissions}
                    unscheduledMissions={unscheduledMissions!}
                    isAlreadyScheduled={isAlreadyScheduled}
                />
            )}
        </>
    )
}
