import { useState, useEffect } from 'react'
import { Deck } from 'models/Deck'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { MissionDefinition } from 'models/MissionDefinition'
import { ScheduleMissionDialog } from './ScheduleMissionDialogs'
import { getInspectionDeadline } from 'utils/StringFormatting'
import { InspectionTable } from './InspectionTable'
import { StyledDict, compareInspections } from './InspectionUtilities'
import { DeckCards } from './DeckCards'
import { Area } from 'models/Area'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'

export interface Inspection {
    missionDefinition: MissionDefinition
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
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const { installationDecks, installationAreas } = useInstallationContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [scrollOnToggle, setScrollOnToggle] = useState<boolean>(true)

    const decks: DeckAreaTuple[] = installationDecks.map((deck) => {
        return {
            areas: installationAreas.filter((a) => a.deckName === deck.deckName),
            deck: deck,
        }
    })

    const deckInspections: DeckInspectionTuple[] =
        decks?.map(({ areas, deck }) => {
            const missionDefinitionsInDeck = missionDefinitions.filter(
                (m) => m.inspectionArea?.deckName === deck.deckName
            )
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

    const onClickDeck = (clickedDeck: Deck) => {
        setSelectedDeck(clickedDeck)
        setScrollOnToggle(!scrollOnToggle)
    }

    const isScheduled = (mission: MissionDefinition) => missionQueue.map((m) => m.missionId).includes(mission.id)
    const isOngoing = (mission: MissionDefinition) => ongoingMissions.map((m) => m.missionId).includes(mission.id)

    const closeDialog = () => {
        setIsAlreadyScheduled(false)
        setSelectedMissions([])
        setIsDialogOpen(false)
    }

    const handleScheduleAll = (inspections: Inspection[]) => {
        setIsDialogOpen(true)
        const sortedInspections = inspections.sort(compareInspections)
        setSelectedMissions(sortedInspections.map((i) => i.missionDefinition))
    }

    useEffect(() => {
        if (selectedMissions && selectedMissions.some((mission) => isOngoing(mission) || isScheduled(mission)))
            setIsAlreadyScheduled(true)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [ongoingMissions, missionQueue, selectedMissions])

    const unscheduledMissions = selectedMissions?.filter((m) => !isOngoing(m) && !isScheduled(m))

    const deck = installationDecks.length === 1 ? installationDecks[0] : selectedDeck
    const inspections =
        deckInspections.length === 1
            ? deckInspections[0].inspections
            : deckInspections.find((d) => d.deck === deck)?.inspections

    const DeckSelection = () => (
        <StyledDict.DeckOverview>
            <DeckCards
                deckMissions={deckInspections}
                onClickDeck={onClickDeck}
                selectedDeck={selectedDeck}
                handleScheduleAll={handleScheduleAll}
            />
        </StyledDict.DeckOverview>
    )

    return (
        <>
            <StyledDict.DeckOverview>
                {installationDecks.length !== 1 && <DeckSelection />}
                {deck && inspections && (
                    <InspectionTable
                        deck={deck}
                        scrollOnToggle={scrollOnToggle}
                        openDialog={() => setIsDialogOpen(true)}
                        setSelectedMissions={setSelectedMissions}
                        inspections={inspections}
                    />
                )}
            </StyledDict.DeckOverview>
            {isDialogOpen && (
                <ScheduleMissionDialog
                    selectedMissions={selectedMissions!}
                    closeDialog={closeDialog}
                    setMissions={setSelectedMissions}
                    unscheduledMissions={unscheduledMissions!}
                    isAlreadyScheduled={isAlreadyScheduled}
                />
            )}
        </>
    )
}
