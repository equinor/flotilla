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
    min-height: 150px;
    padding: 16px;
    flex-direction: column;
    justify-content: space-between;
    flex: 1 0 0;
    cursor: pointer;
    border-radius: 0px 4px 4px 0px;
`

const StyledCardComponent = styled.div`
    display: flex;
    padding-right: 16px;
    justify-content: flex-end;
    gap: 10px;
    width: 100%;
`

const StyledDeckCards = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, 450px);
    gap: 24px;
`

const StyledDeckText = styled.div`
    display: flex;
    flex-direction: column;
    gap: 6px;
    align-self: stretch;
`
const StyledTopDeckText = styled.div`
    display: flex;
    justify-content: space-between;
    margin-right: 5px;
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
    max-width: 450px;
    flex: 1 0 0;
    border-radius: 6px;
    box-shadow: 0px 3px 4px 0px rgba(0, 0, 0, 0.12), 0px 2px 4px 0px rgba(0, 0, 0, 0.14);
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

const StyledMissionInspections = styled.div`
    display: flex;
    flex-direction: column;
    gap: 2px;
`

const StyledPlaceholder = styled.div`
    padding: 24px;
    border: 1px solid #dcdcdc;
    border-radius: 4px;
`

const StyledContent = styled.div`
    display: flex;
    align-items: centre;
    gap: 5px;
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

export interface DeckMissionCount {
    [color: string]: {
        count: number
        message: string
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

export const compareInspections = (i1: Inspection, i2: Inspection) => {
    if (!i1.missionDefinition.inspectionFrequency) return 1
    if (!i2.missionDefinition.inspectionFrequency) return -1
    if (!i1.missionDefinition.lastRun) return -1
    if (!i2.missionDefinition.lastRun) return 1
    else return i1.deadline!.getTime() - i2.deadline!.getTime()
}

export function InspectionSection({
    refreshInterval,
    updateScheduledMissionsMap,
    scheduledMissions,
    ongoingMissions,
}: IInspectionProps) {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [deckMissions, setDeckMissions] = useState<DeckMissionType>({})
    const [selectedDeck, setSelectedDeck] = useState<Deck>()
    const [selectedMissions, setSelectedMissions] = useState<CondensedMissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isScheduledDialogOpen, setIsScheduledDialogOpen] = useState<boolean>(false)

    const openDialog = () => {
        setIsDialogOpen(true)
    }

    const openScheduleDialog = () => {
        setIsScheduledDialogOpen(true)
    }

    const closeDialog = () => {
        setIsDialogOpen(false)
    }

    const closeScheduleDialog = () => {
        setIsScheduledDialogOpen(false)
    }

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
        inspections.sort(compareInspections)
        setSelectedMissions(inspections.map((i) => i.missionDefinition))
    }

    useEffect(() => {
        const id = setInterval(() => {
            updateScheduledMissionsMap(deckMissions)
        }, refreshInterval)
        return () => clearInterval(id)
    }, [deckMissions])

    const getDeadlineInspection = (deadline: Date) => {
        const deadlineDays = getDeadlineInDays(deadline)
        switch (true) {
            case deadlineDays <= 1:
                return 'red'
            case deadlineDays > 1 && deadlineDays <= 7:
                return 'orange'
            case deadlineDays > 7 && deadlineDays <= 14:
                return 'orange'
            case deadlineDays > 7 && deadlineDays <= 30:
                return 'green'
        }
        return 'green'
    }

    const getCardColor = (deckId: string) => {
        const inspections = deckMissions[deckId].inspections
        if (inspections.length === 0) return 'gray'
        const sortedInspections = inspections.sort(compareInspections)

        if (sortedInspections.length === 0) return 'green'

        const nextInspection = sortedInspections[0]

        if (!nextInspection.deadline) {
            if (!nextInspection.missionDefinition.inspectionFrequency) return 'gray'
            else return 'red'
        }

        return getDeadlineInspection(nextInspection.deadline)
    }

    function getCardMissionInformation(deckId: string) {
        var colorsCount: DeckMissionCount = {
            red: { count: 0, message: 'Must be inspected this week' },
            orange: { count: 0, message: 'Must be inspected within two weeks' },
            green: { count: 0, message: 'Up to date' },
            grey: { count: 0, message: '' },
        }
        const inspections = deckMissions[deckId].inspections
        if (inspections.length === 0) return

        deckMissions[deckId].inspections.map((inspection) => {
            if (!inspection.deadline) {
                if (!inspection.missionDefinition.lastRun && inspection.missionDefinition.inspectionFrequency) {
                    colorsCount['red'].count++
                    return
                }
                colorsCount['green'].count++
                return
            }
            const dealineColor = getDeadlineInspection(inspection.deadline)
            colorsCount[dealineColor!].count++
            return
        })

        return (
            <StyledMissionInspections>
                {Object.keys(colorsCount).map((color) => (
                    <>
                        {colorsCount[color].count > 0 && (
                            <StyledMissionComponents>
                                <StyledCircle style={{ background: color }} />
                                <Typography color={tokens.colors.text.static_icons__secondary.rgba}>
                                    {colorsCount[color].count > 1 &&
                                        colorsCount[color].count +
                                            ' ' +
                                            TranslateText('Missions').toLowerCase() +
                                            ' ' +
                                            TranslateText(colorsCount[color].message).toLowerCase()}
                                    {colorsCount[color].count === 1 &&
                                        colorsCount[color].count +
                                            ' ' +
                                            TranslateText('Mission').toLowerCase() +
                                            ' ' +
                                            TranslateText(colorsCount[color].message).toLowerCase()}
                                </Typography>
                            </StyledMissionComponents>
                        )}
                    </>
                ))}
            </StyledMissionInspections>
        )
    }

    return (
        <>
            <StyledDeckOverview>
                <StyledDeckCards>
                    {Object.keys(deckMissions).length > 0 ? (
                        Object.keys(deckMissions).map((deckId) => (
                            <DeckCard>
                                <Rectangle style={{ background: `${getCardColor(deckId)}` }} />
                                <StyledCard
                                    variant="default"
                                    key={deckId}
                                    onClick={() => setSelectedDeck(deckMissions[deckId].deck)}
                                    style={
                                        selectedDeck === deckMissions[deckId].deck
                                            ? { border: `solid ${getCardColor(deckId)} 2px` }
                                            : {}
                                    }
                                >
                                    <StyledDeckText>
                                        <StyledTopDeckText>
                                            <Typography variant={'body_short_bold'}>
                                                {deckMissions[deckId].deck.deckName.toString()}
                                            </Typography>
                                            {deckMissions[deckId].inspections.map(
                                                (inspection) =>
                                                    Object.keys(ongoingMissions).includes(
                                                        inspection.missionDefinition.id
                                                    ) && (
                                                        <StyledContent>
                                                            <Icon name={Icons.Ongoing} size={16} />
                                                            {TranslateText('InProgress')}
                                                        </StyledContent>
                                                    )
                                            )}
                                        </StyledTopDeckText>
                                        {getCardMissionInformation(deckId)}
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
                        <StyledPlaceholder>
                            <Typography variant="h4" color="disabled">
                                {TranslateText('No deck inspections available')}
                            </Typography>
                        </StyledPlaceholder>
                    )}
                </StyledDeckCards>
                {selectedDeck && (
                    <InspectionTable
                        deck={selectedDeck}
                        openDialog={openDialog}
                        setSelectedMissions={setSelectedMissions}
                        inspections={deckMissions[selectedDeck.id].inspections}
                        scheduledMissions={scheduledMissions}
                        ongoingMissions={ongoingMissions}
                        isScheduledDialogOpen={isScheduledDialogOpen}
                        openScheduleDialog={openScheduleDialog}
                        closeScheduleDialog={closeScheduleDialog}
                    />
                )}
            </StyledDeckOverview>
            {isDialogOpen && (
                <ScheduleMissionDialog
                    missions={selectedMissions!}
                    refreshInterval={refreshInterval}
                    closeDialog={closeDialog}
                    setMissions={setSelectedMissions}
                />
            )}
        </>
    )
}
