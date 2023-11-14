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
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { Area } from 'models/Area'

export interface Inspection {
    missionDefinition: CondensedMissionDefinition
    deadline: Date | undefined
}

interface DeckInspectionTuple {
    areas: Area[]
    inspections: Inspection[]
    deck: Deck
}

export interface DeckMissionType {
    [deckName: string]: DeckInspectionTuple
}

export interface DeckMissionCount {
    [color: string]: {
        count: number
        message: string
    }
}

export interface DeckAreas {
    [deckId: string]: {
        areaString: string
    }
}

export interface ScheduledMissionType {
    [missionId: string]: boolean
}

interface IInspectionProps {
    scheduledMissions: ScheduledMissionType
    ongoingMissions: ScheduledMissionType
}

export function InspectionSection({ scheduledMissions, ongoingMissions }: IInspectionProps) {
    const { installationCode } = useInstallationContext()
    const [deckMissions, setDeckMissions] = useState<DeckMissionType>({})
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [unscheduledMissions, setUnscheduledMissions] = useState<CondensedMissionDefinition[]>([])
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)
    const { registerEvent, connectionReady } = useSignalRContext()

    const openDialog = () => {
        setIsDialogOpen(true)
    }

    const closeDialog = () => {
        setIsAlreadyScheduled(false)
        setSelectedMissions([])
        setUnscheduledMissions([])
        setIsDialogOpen(false)
    }

    useEffect(() => {
        if (selectedMissions) {
            let unscheduledMissions: CondensedMissionDefinition[] = []
            selectedMissions.forEach((mission) => {
                if (Object.keys(scheduledMissions).includes(mission.id) && scheduledMissions[mission.id])
                    setIsAlreadyScheduled(true)
                else unscheduledMissions = unscheduledMissions.concat([mission])
            })
            setUnscheduledMissions(unscheduledMissions)
        }
    }, [isDialogOpen, scheduledMissions, selectedMissions])

    useMemo(() => {
        const updateDeckMissions = () => {
            BackendAPICaller.getDecks().then(async (decks: Deck[]) => {
                let newDeckMissions: DeckMissionType = {}
                const filteredDecks = decks.filter(
                    (deck) => deck.installationCode.toLowerCase() === installationCode.toLowerCase()
                )
                for (const deck of filteredDecks) {
                    let areasInDeck = await BackendAPICaller.getAreasByDeckId(deck.id)
                    console.log(areasInDeck)

                    // These calls need to be made sequentially to update areaMissions safely
                    let missionDefinitions = await BackendAPICaller.getMissionDefinitionsInDeck(deck)
                    if (!missionDefinitions) missionDefinitions = []
                    newDeckMissions[deck.deckName] = {
                        inspections: missionDefinitions.map((m) => {
                            return {
                                missionDefinition: m,
                                deadline: m.lastSuccessfulRun
                                    ? getInspectionDeadline(m.inspectionFrequency, m.lastSuccessfulRun.endTime!)
                                    : undefined,
                            }
                        }),
                        areas: areasInDeck,
                        deck: deck,
                    }
                }
                setDeckMissions(newDeckMissions)
            })
        }
        setSelectedDeck(undefined)
        updateDeckMissions()
    }, [installationCode])

    useEffect(() => {
        const updateDeckInspectionsWithUpdatedMissionDefinition = (
            mDef: CondensedMissionDefinition,
            relevantDeck: DeckInspectionTuple
        ) => {
            const inspections = relevantDeck.inspections
            const index = inspections.findIndex((i) => i.missionDefinition.id === mDef.id)
            if (index !== -1) {
                // Ignore mission definitions for other decks
                inspections[index] = {
                    missionDefinition: mDef,
                    deadline: mDef.lastSuccessfulRun // If there are no completed runs, set the deadline to undefined
                        ? getInspectionDeadline(mDef.inspectionFrequency, mDef.lastSuccessfulRun.endTime!)
                        : undefined,
                }
                relevantDeck = { ...relevantDeck, inspections: inspections }
            }
            return relevantDeck
        }

        if (connectionReady) {
            registerEvent(SignalREventLabels.missionDefinitionUpdated, (username: string, message: string) => {
                const mDef: CondensedMissionDefinition = JSON.parse(message)
                if (!mDef.area) return
                const relevantDeckName = mDef.area.deckName
                setDeckMissions((deckMissions) => {
                    return {
                        ...deckMissions,
                        [relevantDeckName]: updateDeckInspectionsWithUpdatedMissionDefinition(
                            mDef,
                            deckMissions[relevantDeckName]
                        ),
                    }
                })
            })
        }
    }, [registerEvent, connectionReady])

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
                    ongoingMissions={ongoingMissions}
                    handleScheduleAll={handleScheduleAll}
                />
                {selectedDeck && (
                    <InspectionTable
                        deck={selectedDeck}
                        openDialog={openDialog}
                        setSelectedMissions={setSelectedMissions}
                        inspections={deckMissions[selectedDeck.deckName].inspections}
                        scheduledMissions={scheduledMissions}
                        ongoingMissions={ongoingMissions}
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
