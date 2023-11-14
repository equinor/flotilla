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

    const getCardColor = (deckName: string) => {
        const inspections = deckMissions[deckName].inspections
        if (inspections.length === 0) return 'gray'
        const sortedInspections = inspections.sort(compareInspections)

        if (sortedInspections.length === 0) return 'green'

        const nextInspection = sortedInspections[0]

        if (!nextInspection.deadline) {
            if (!nextInspection.missionDefinition.inspectionFrequency) return 'green'
            else return 'red'
        }

        return getDeadlineInspection(nextInspection.deadline)
    }

    return (
        <StyledDict.DeckCards>
            {Object.keys(deckMissions).length > 0 ? (
                Object.keys(deckMissions).map((deckName) => (
                    <StyledDict.DeckCard key={deckName}>
                        <StyledDict.Rectangle style={{ background: `${getCardColor(deckName)}` }} />
                        <StyledDict.Card
                            key={deckName}
                            onClick={
                                deckMissions[deckName].inspections.length > 0
                                    ? () => setSelectedDeck(deckMissions[deckName].deck)
                                    : undefined
                            }
                            style={
                                selectedDeck === deckMissions[deckName].deck
                                    ? { border: `solid ${getCardColor(deckName)} 2px` }
                                    : {}
                            }
                        >
                            <StyledDict.DeckText>
                                <StyledDict.TopDeckText>
                                    <Typography variant={'body_short_bold'}>{deckName.toString()}</Typography>
                                    {deckMissions[deckName].inspections
                                        .filter((i) => Object.keys(ongoingMissions).includes(i.missionDefinition.id))
                                        .map((inspection) => (
                                            <StyledDict.Content key={inspection.missionDefinition.id}>
                                                <Icon name={Icons.Ongoing} size={16} />
                                                {TranslateText('InProgress')}
                                            </StyledDict.Content>
                                        ))}
                                </StyledDict.TopDeckText>
                                {deckMissions[deckName].areas && (
                                    <Typography variant={'body_short'}>
                                        {deckMissions[deckName].areas
                                            .map((area) => {
                                                return area.areaName.toLocaleUpperCase()
                                            })
                                            .sort()
                                            .join(' | ')}
                                    </Typography>
                                )}
                                {deckMissions[deckName].inspections && (
                                    <CardMissionInformation deckName={deckName} deckMissions={deckMissions} />
                                )}
                            </StyledDict.DeckText>
                            <StyledDict.CardComponent>
                                <Tooltip
                                    placement="top"
                                    title={
                                        deckMissions[deckName].inspections.length > 0
                                            ? ''
                                            : TranslateText('No planned inspection')
                                    }
                                >
                                    <Button
                                        disabled={deckMissions[deckName].inspections.length === 0}
                                        variant="outlined"
                                        onClick={() => handleScheduleAll(deckMissions[deckName].inspections)}
                                        color="secondary"
                                    >
                                        <Icon
                                            name={Icons.LibraryAdd}
                                            color={deckMissions[deckName].inspections.length > 0 ? '' : 'grey'}
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
    )
}
