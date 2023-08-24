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
import { getInspectionDeadline } from 'utils/StringFormatting'
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

export interface DeckMissionType {
    [deckId: string]: { missionDefinitions: CondensedMissionDefinition[]; deck: Deck }
}

export interface OngoingMissionType {
    [missionId: string]: boolean
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
                {selectedDeck && 
                    <InspectionTable 
                        deck={selectedDeck} 
                        openDialog={() => setisDialogOpen(true)}
                        setSelectedMissions={setSelectedMissions}
                        deckMissions={deckMissions}
                        ongoingMissions={ongoingMissions} />
                }
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
