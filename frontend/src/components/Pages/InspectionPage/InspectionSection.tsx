import { Card, Typography, Button, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Deck } from 'models/Deck'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { tokens } from '@equinor/eds-tokens'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { getDeadlineInDays, getInspectionDeadline } from 'utils/StringFormatting'
import { InspectionTable } from './InspectionTable'
import { Icons } from 'utils/icons'

const StyledCard = styled(Card)`
    display: flex;
    height: 150px;
    padding: 16px;
    flex-direction: column;
    justify-content: space-between;
    align-items: flex-start;
    flex: 1 0 0;
`

const StyledCardComponent = styled.div`
    display: flex;
    padding-right: 16px;
    justify-content: flex-end;
    gap: 10px;
    width: 100%;
    border-radius: 4px;
`

const StyledDeckCards = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, 400px);
    gap: 24px;
`

const StyledDeckText = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    align-self: stretch;
`

const Rectangle = styled.div`
    display: flex-start;
    width: 24px;
    height: 100%;
    border-radius: 6px 0px 0px 6px;
`

const DeckCard = styled.div`
    display: flex;
    min-width: 400px;
    max-width: 400px;
    align-items: flex-start;
    flex: 1 0 0;
    border-radius: 6px;
`

const StyledCircle = styled.div`
    width: 13px;
    height: 13px;
    border-radius: 100px;
`

const StyledMissionComponents = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 4px;
`

const StyledDeckOverview = styled.div`
    display: flex;
    flex-direction: column;
    gap: 25px;
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

interface IInspectionProps {
    refreshInterval: number
    updateOngoingMissionsMap: (areaMissions: DeckMissionType) => Promise<void>
    ongoingMissions: OngoingMissionType
}

export const compareInspections = (i1: Inspection, i2: Inspection) => {
    if (!i1.missionDefinition.inspectionFrequency) return 1
    if (!i2.missionDefinition.inspectionFrequency) return -1
    if (!i1.missionDefinition.lastRun) return 1
    if (!i2.missionDefinition.lastRun) return -1
    else return i1.deadline!.getTime() - i2.deadline!.getTime()
}

export function InspectionSection({ refreshInterval, updateOngoingMissionsMap, ongoingMissions }: IInspectionProps) {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [deckMissions, setDeckMissions] = useState<DeckMissionType>({})
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
                return 'orange'
            case deadlineDays > 7:
                return 'green'
        }
    }

    return (
        <>
            <StyledDeckOverview>
                <StyledDeckCards>
                    {Object.keys(deckMissions).length > 0 ? (
                        Object.keys(deckMissions).map((deckId) => (
                            <DeckCard
                                style={{
                                    boxShadow:
                                        '0px 3px 4px 0px rgba(0, 0, 0, 0.12), 0px 2px 4px 0px rgba(0, 0, 0, 0.14)',
                                }}
                            >
                                <Rectangle style={{ background: `${getCardColor(deckId)}` }} />
                                <StyledCard
                                    variant="default"
                                    key={deckId}
                                    onClick={() => setSelectedDeck(deckMissions[deckId].deck)}
                                >
                                    <StyledDeckText>
                                        <Typography variant={'body_short_bold'}>
                                            {deckMissions[deckId].deck.deckName.toString()}
                                        </Typography>
                                        <StyledMissionComponents>
                                            <StyledCircle style={{ background: `${getCardColor(deckId)}` }} />
                                            <Typography color={tokens.colors.text.static_icons__secondary.rgba}>
                                                {deckMissions[deckId] &&
                                                    deckMissions[deckId].inspections.length > 0 &&
                                                    deckMissions[deckId].inspections.length +
                                                        ' ' +
                                                        TranslateText('Missions')}
                                            </Typography>
                                        </StyledMissionComponents>
                                    </StyledDeckText>
                                    <StyledCardComponent>
                                        <Button
                                            variant="outlined"
                                            onClick={() => handleScheduleAll(deckMissions[deckId].inspections)}
                                            style={{ borderColor: '#3D3D3D' }}
                                        >
                                            <Icon
                                                name={Icons.LibraryAdd}
                                                color={tokens.colors.text.static_icons__default.rgba}
                                            />
                                            <Typography color={tokens.colors.text.static_icons__secondary.rgba}>
                                                {TranslateText('Queue the missions')}
                                            </Typography>
                                        </Button>
                                    </StyledCardComponent>
                                </StyledCard>
                            </DeckCard>
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
            </StyledDeckOverview>
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
