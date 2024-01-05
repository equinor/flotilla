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

export interface DeckMissionCount {
    [color: string]: {
        count: number
        message: string
    }
}

export const InspectionSection = () => {
    const { installationCode } = useInstallationContext()
    const [deckMissions, setDeckMissions] = useState<DeckInspectionTuple[]>([])
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [unscheduledMissions, setUnscheduledMissions] = useState<CondensedMissionDefinition[]>([])
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const { missionDefinitions } = useMissionDefinitionsContext()

    const closeDialog = () => {
        setIsAlreadyScheduled(false)
        setSelectedMissions([])
        setUnscheduledMissions([])
        setIsDialogOpen(false)
    }

    useEffect(() => {
        const isScheduled = (mission: CondensedMissionDefinition) =>
            missionQueue.map((m) => m.missionId).includes(mission.id)
        const isOngoing = (mission: CondensedMissionDefinition) =>
            ongoingMissions.map((m) => m.missionId).includes(mission.id)

        if (selectedMissions) {
            let unscheduledMissions: CondensedMissionDefinition[] = []
            selectedMissions.forEach((mission) => {
                if (isOngoing(mission) || isScheduled(mission)) setIsAlreadyScheduled(true)
                else unscheduledMissions = unscheduledMissions.concat([mission])
            })
            setUnscheduledMissions(unscheduledMissions)
        }
    }, [isDialogOpen, ongoingMissions, missionQueue, selectedMissions])

    useMemo(() => {
        const updateDeckMissions = () =>
            BackendAPICaller.getDecks().then(async (decks: Deck[]) =>
                setDeckMissions(
                    await Promise.all(
                        // This is needed since the map function uses async calls
                        decks
                            .filter((deck) => deck.installationCode.toLowerCase() === installationCode.toLowerCase())
                            .map(async (deck) => {
                                let missionDefinitionsInDeck =
                                    missionDefinitions.filter((m) => m.area?.deckName === deck.deckName) ?? []
                                return {
                                    inspections: missionDefinitionsInDeck.map((m) => {
                                        return {
                                            missionDefinition: m,
                                            deadline: m.lastSuccessfulRun
                                                ? getInspectionDeadline(
                                                      m.inspectionFrequency,
                                                      m.lastSuccessfulRun.endTime!
                                                  )
                                                : undefined,
                                        }
                                    }),
                                    areas: await BackendAPICaller.getAreasByDeckId(deck.id),
                                    deck: deck,
                                }
                            })
                    )
                )
            )

        if (deckMissions.length === 0 && missionDefinitions) {
            setSelectedDeck(undefined)
            updateDeckMissions()
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [installationCode, missionDefinitions])

    useEffect(() => {
        const updateInspections = (oldInspections: Inspection[], mDefs: CondensedMissionDefinition[]) => {
            return [...oldInspections].map((inspection) => {
                const updatedMDef = mDefs.find((m) => m.id === inspection.missionDefinition.id)
                if (updatedMDef) {
                    const newDeadline = updatedMDef.lastSuccessfulRun // If there are no completed runs, set the deadline to undefined
                        ? getInspectionDeadline(updatedMDef.inspectionFrequency, updatedMDef.lastSuccessfulRun.endTime!)
                        : undefined
                    return {
                        ...inspection,
                        missionDefinition: updatedMDef,
                        deadline: newDeadline,
                    }
                }
                return inspection
            })
        }

        if (deckMissions) {
            setDeckMissions((deckMissions) =>
                [...deckMissions].map((deckMission) => {
                    const relevantMissionDefinitions = missionDefinitions.filter(
                        (m) => m.area?.deckName === deckMission.deck.deckName
                    )
                    return {
                        ...deckMission,
                        inspections: updateInspections(deckMission.inspections, relevantMissionDefinitions),
                    }
                })
            )
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [missionDefinitions])

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
