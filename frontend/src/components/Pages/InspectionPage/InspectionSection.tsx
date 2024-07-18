import { useState, useEffect } from 'react'
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
    const { installationCode, installationDecks, installationAreas } = useInstallationContext()
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)
    const [scrollOnToggle, setScrollOnToggle] = useState<boolean>(true)
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const { missionDefinitions } = useMissionDefinitionsContext()

    const decks: DeckAreaTuple[] = installationDecks
        .filter((deck) => deck.installationCode.toLowerCase() === installationCode.toLowerCase())
        .map((deck) => {
            return {
                areas: installationAreas.filter((a) => a.deckName === deck.deckName),
                deck: deck,
            }
        })

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

    const deckMissions: DeckInspectionTuple[] =
        decks?.map(({ areas, deck }) => {
            const missionDefinitionsInDeck = missionDefinitions.filter(
                (m) => m.area?.deckName === deck.deckName && m.installationCode === installationCode
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

    const handleScheduleAll = (inspections: Inspection[]) => {
        setIsDialogOpen(true)
        const sortedInspections = inspections.sort(compareInspections)
        setSelectedMissions(sortedInspections.map((i) => i.missionDefinition))
    }

    const onClickDeck = (clickedDeck: Deck) => {
        setSelectedDeck(clickedDeck)
        setScrollOnToggle(!scrollOnToggle)
    }

    return (
        <>
            <StyledDict.DeckOverview>
                <DeckCards
                    deckMissions={deckMissions}
                    onClickDeck={onClickDeck}
                    selectedDeck={selectedDeck}
                    handleScheduleAll={handleScheduleAll}
                />
                {selectedDeck && (
                    <InspectionTable
                        deck={selectedDeck}
                        scrollOnToggle={scrollOnToggle}
                        openDialog={() => setIsDialogOpen(true)}
                        setSelectedMissions={setSelectedMissions}
                        inspections={deckMissions.find((d) => d.deck.deckName === selectedDeck.deckName)!.inspections}
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
