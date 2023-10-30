import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Deck } from 'models/Deck'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { ScheduleMissionDialog } from './ScheduleMissionDialogs'
import { getInspectionDeadline } from 'utils/StringFormatting'
import { InspectionTable } from './InspectionTable'
import { StyledDict, compareInspections } from './InspectionUtilities'
import { DeckCards } from './DeckCards'

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
    refreshInterval: number
    updateScheduledMissionsMap: (areaMissions: DeckMissionType) => Promise<void>
    scheduledMissions: ScheduledMissionType
    ongoingMissions: ScheduledMissionType
}

export function InspectionSection({
    refreshInterval,
    updateScheduledMissionsMap,
    scheduledMissions,
    ongoingMissions,
}: IInspectionProps) {
    const { installationCode } = useInstallationContext()
    const [deckMissions, setDeckMissions] = useState<DeckMissionType>({})
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [unscheduledMissions, setUnscheduledMissions] = useState<CondensedMissionDefinition[]>([])
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)

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
    }, [isDialogOpen])

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
        openDialog()
        const sortedInspections = inspections.sort(compareInspections)
        setSelectedMissions(sortedInspections.map((i) => i.missionDefinition))
    }

    useEffect(() => {
        const id = setInterval(() => {
            updateScheduledMissionsMap(deckMissions)
        }, refreshInterval)
        return () => clearInterval(id)
    }, [deckMissions])

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
                        inspections={deckMissions[selectedDeck.id].inspections}
                        scheduledMissions={scheduledMissions}
                        ongoingMissions={ongoingMissions}
                    />
                )}
            </StyledDict.DeckOverview>
            {isDialogOpen && (
                <ScheduleMissionDialog
                    missions={selectedMissions!}
                    refreshInterval={refreshInterval}
                    closeDialog={closeDialog}
                    setMissions={setSelectedMissions}
                    unscheduledMissions={unscheduledMissions!}
                    isAlreadyScheduled={isAlreadyScheduled}
                />
            )}
        </>
    )
}
