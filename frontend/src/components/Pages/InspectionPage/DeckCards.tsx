import { Deck } from 'models/Deck'
import { DeckInspectionTuple, Inspection } from './InspectionSection'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    CardMissionInformation,
    DeckCardColors,
    StyledDict,
    compareInspections,
    getDeadlineInspection,
} from './InspectionUtilities'
import { Button, Icon, Tooltip, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useRobotContext } from 'components/Contexts/RobotContext'

interface IDeckCardProps {
    deckMissions: DeckInspectionTuple[]
    onClickDeck: (deck: Deck) => void
    selectedDeck: Deck | undefined
    handleScheduleAll: (inspections: Inspection[]) => void
}

interface DeckCardProps {
    deckData: DeckInspectionTuple
    onClickDeck: (deck: Deck) => void
    selectedDeck: Deck | undefined
    handleScheduleAll: (inspections: Inspection[]) => void
}

const DeckCard = ({ deckData, onClickDeck, selectedDeck, handleScheduleAll }: DeckCardProps) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions } = useMissionsContext()
    const { enabledRobots } = useRobotContext()

    const isScheduleMissionsDisabled = enabledRobots.length === 0 || deckData.inspections.length === 0

    const getCardColorFromInspections = (inspections: Inspection[]): DeckCardColors => {
        if (inspections.length === 0) return DeckCardColors.Gray
        const sortedInspections = inspections.sort(compareInspections)

        if (sortedInspections.length === 0) return DeckCardColors.Green

        const nextInspection = sortedInspections[0]
        if (!nextInspection.deadline) {
            if (!nextInspection.missionDefinition.inspectionFrequency) return DeckCardColors.Green
            else return DeckCardColors.Red
        }

        return getDeadlineInspection(nextInspection.deadline)
    }

    let queueMissionsTooltip = ''
    if (deckData.inspections.length === 0) queueMissionsTooltip = TranslateText('No planned inspection')
    else if (isScheduleMissionsDisabled) queueMissionsTooltip = TranslateText('No robot available')

    const formattedAreaNames = deckData.areas
        .map((area) => area.areaName.toLocaleUpperCase())
        .sort()
        .join(' | ')

    return (
        <StyledDict.DeckCard key={deckData.deck.deckName}>
            <StyledDict.Rectangle style={{ background: `${getCardColorFromInspections(deckData.inspections)}` }} />
            <StyledDict.Card
                key={deckData.deck.deckName}
                onClick={deckData.inspections.length > 0 ? () => onClickDeck(deckData.deck) : undefined}
                style={
                    selectedDeck === deckData.deck
                        ? { border: `solid ${getCardColorFromInspections(deckData.inspections)} 2px` }
                        : {}
                }
            >
                <StyledDict.DeckText>
                    <StyledDict.TopDeckText>
                        <Typography variant={'body_short_bold'}>{deckData.deck.deckName.toString()}</Typography>
                        {deckData.inspections
                            .filter((i) => ongoingMissions.find((m) => m.missionId === i.missionDefinition.id))
                            .map((inspection) => (
                                <StyledDict.Content key={inspection.missionDefinition.id}>
                                    <Icon name={Icons.Ongoing} size={16} />
                                    {TranslateText('InProgress')}
                                </StyledDict.Content>
                            ))}
                    </StyledDict.TopDeckText>
                    {deckData.areas && <Typography variant={'body_short'}>{formattedAreaNames}</Typography>}
                    {deckData.inspections && (
                        <CardMissionInformation deckName={deckData.deck.deckName} inspections={deckData.inspections} />
                    )}
                </StyledDict.DeckText>
                <StyledDict.CardComponent>
                    <Tooltip placement="top" title={queueMissionsTooltip}>
                        <Button
                            disabled={isScheduleMissionsDisabled}
                            variant="outlined"
                            onClick={() => handleScheduleAll(deckData.inspections)}
                            color="secondary"
                        >
                            <Icon name={Icons.LibraryAdd} color={deckData.inspections.length > 0 ? '' : 'grey'} />
                            <Typography color={tokens.colors.text.static_icons__secondary.rgba}>
                                {TranslateText('Queue the missions')}
                            </Typography>
                        </Button>
                    </Tooltip>
                </StyledDict.CardComponent>
            </StyledDict.Card>
        </StyledDict.DeckCard>
    )
}

export const DeckCards = ({ deckMissions, onClickDeck, selectedDeck, handleScheduleAll }: IDeckCardProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            {Object.keys(deckMissions).length > 0 ? (
                <StyledDict.DeckCards>
                    {deckMissions.map((deckMission) => (
                        <DeckCard
                            key={'deckCard' + deckMission.deck.deckName}
                            deckData={deckMission}
                            onClickDeck={onClickDeck}
                            selectedDeck={selectedDeck}
                            handleScheduleAll={handleScheduleAll}
                        />
                    ))}
                </StyledDict.DeckCards>
            ) : (
                <StyledDict.Placeholder>
                    <Typography variant="h4" color="disabled">
                        {TranslateText('No deck inspections available')}
                    </Typography>
                </StyledDict.Placeholder>
            )}
        </>
    )
}
