import { Deck } from 'models/Deck'
import { DeckAreas, DeckMissionType, Inspection, ScheduledMissionType } from './InspectionSection'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { CardMissionInformation, StyledDict, compareInspections, getDeadlineInspection } from './InspectionUtilities'
import { Button, Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useEffect, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'

interface IDeckCardProps {
    deckMissions: DeckMissionType
    setSelectedDeck: React.Dispatch<React.SetStateAction<Deck | undefined>>
    selectedDeck: Deck | undefined
    ongoingMissions: ScheduledMissionType
    handleScheduleAll: (inspections: Inspection[]) => void
}

export function DeckCards({
    deckMissions,
    setSelectedDeck,
    selectedDeck,
    ongoingMissions,
    handleScheduleAll,
}: IDeckCardProps) {
    const { TranslateText } = useLanguageContext()
    const [areas, setAreas] = useState<DeckAreas>({})

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

    useEffect(() => {
        const newAreas: DeckAreas = {}

        Object.keys(deckMissions).forEach((deckId) => {
            BackendAPICaller.getAreasByDeckId(deckMissions[deckId].deck.id).then((areas) => {
                const formattedAreaNames = areas
                    .map((area) => {
                        return area.areaName.toLocaleUpperCase()
                    })
                    .sort()
                    .join(' | ')
                newAreas[deckMissions[deckId].deck.id] = {
                    areaString: formattedAreaNames,
                }
            })
        })
        setAreas(newAreas)
    }, [deckMissions])

    return (
        <>
            <StyledDict.DeckCards>
                {Object.keys(deckMissions).length > 0 ? (
                    Object.keys(deckMissions).map((deckId) => (
                        <StyledDict.DeckCard key={deckId}>
                            <StyledDict.Rectangle style={{ background: `${getCardColor(deckId)}` }} />
                            <StyledDict.Card
                                key={deckId}
                                onClick={
                                    deckMissions[deckId].inspections.length > 0
                                        ? () => setSelectedDeck(deckMissions[deckId].deck)
                                        : undefined
                                }
                                style={
                                    selectedDeck === deckMissions[deckId].deck
                                        ? { border: `solid ${getCardColor(deckId)} 2px` }
                                        : {}
                                }
                            >
                                <StyledDict.DeckText>
                                    <StyledDict.TopDeckText>
                                        <Typography variant={'body_short_bold'}>
                                            {deckMissions[deckId].deck.deckName.toString()}
                                        </Typography>
                                        {deckMissions[deckId].inspections
                                            .filter((i) =>
                                                Object.keys(ongoingMissions).includes(i.missionDefinition.id)
                                            )
                                            .map((inspection) => (
                                                <StyledDict.Content key={inspection.missionDefinition.id}>
                                                    <Icon name={Icons.Ongoing} size={16} />
                                                    {TranslateText('InProgress')}
                                                </StyledDict.Content>
                                            ))}
                                    </StyledDict.TopDeckText>
                                    {Object.keys(areas).includes(deckId) && (
                                        <Typography variant={'body_short'}>{areas[deckId].areaString}</Typography>
                                    )}
                                    {deckMissions[deckId].inspections && (
                                        <CardMissionInformation deckId={deckId} deckMissions={deckMissions} />
                                    )}
                                </StyledDict.DeckText>
                                <StyledDict.CardComponent>
                                    <Tooltip
                                        placement="top"
                                        title={
                                            deckMissions[deckId].inspections.length > 0
                                                ? ''
                                                : TranslateText('No planned inspection')
                                        }
                                    >
                                        <Button
                                            disabled={deckMissions[deckId].inspections.length === 0}
                                            variant="outlined"
                                            onClick={() => handleScheduleAll(deckMissions[deckId].inspections)}
                                            color="secondary"
                                        >
                                            <Icon
                                                name={Icons.LibraryAdd}
                                                color={deckMissions[deckId].inspections.length > 0 ? '' : 'grey'}
                                            />
                                            <Typography color={tokens.colors.text.static_icons__secondary.rgba}>
                                                {TranslateText('Queue the missions')}
                                            </Typography>
                                        </Button>
                                    </Tooltip>
                                </StyledDict.CardComponent>
                            </StyledDict.Card>
                        </StyledDict.DeckCard>
                    ))
                ) : (
                    <StyledDict.Placeholder>
                        <Typography variant="h4" color="disabled">
                            {TranslateText('No deck inspections available')}
                        </Typography>
                    </StyledDict.Placeholder>
                )}
            </StyledDict.DeckCards>
        </>
    )
}
