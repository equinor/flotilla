import { Button, Icon, Table } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import {
    allDays,
    getAllDaysIndexOfToday,
    getTimeMissionPairsForDay,
    isJobScheduledAt,
} from 'models/AutoScheduleFrequency'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { AutoScheduleMissionTableRow } from './AutoScheduleMissionTableRow'
import { useNow } from 'hooks/useNow'

const StyledNextAutoMission = styled.div`
    margin-top: 30px;
`

const SectionTitle = styled.p`
    margin: 0 0 10px 0;
    font-family: Equinor, sans-serif;
    font-size: 0.92rem;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__default.hex};
`

const StyledTable = styled(Table)`
    width: 960px;
    margin-top: 10px;
    border-top: 1px solid ${tokens.colors.ui.background__medium.hex};

    @media (max-width: 960px) {
        width: 100%;
    }
`

const ShowLessOrMoreButton = ({
    showMore,
    setShowMore,
}: {
    showMore: boolean
    setShowMore: (newShowMore: boolean) => void
}) => {
    const { TranslateText } = useLanguageContext()

    if (showMore) {
        return (
            <Button
                variant="ghost"
                onClick={() => {
                    setShowMore(false)
                }}
            >
                <Icon name={Icons.UpChevron} size={16} />
                {TranslateText('Show less')}
            </Button>
        )
    }
    return (
        <Button
            variant="ghost"
            onClick={() => {
                setShowMore(true)
            }}
        >
            <Icon name={Icons.DownChevron} size={16} />
            {TranslateText('Show more')}
        </Button>
    )
}

export const NextAutoScheduleMissionView = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [showMore, setShowMore] = useState(false)

    const now = useNow()
    const currentDayOfTheWeek = allDays[getAllDaysIndexOfToday(now)]

    const timeMissionPairs = getTimeMissionPairsForDay(missionDefinitions, currentDayOfTheWeek).filter(
        ({ time, mission }) => isJobScheduledAt(mission, time)
    )

    return (
        <>
            {timeMissionPairs.length > 0 && (
                <StyledNextAutoMission>
                    <SectionTitle>{TranslateText('Next auto scheduled mission for today')}</SectionTitle>
                    <StyledTable>
                        <Table.Body style={{ backgroundColor: tokens.colors.ui.background__default.hex }}>
                            {!showMore ? (
                                <AutoScheduleMissionTableRow
                                    day={currentDayOfTheWeek}
                                    time={timeMissionPairs[0].time}
                                    mission={timeMissionPairs[0].mission}
                                />
                            ) : (
                                <>
                                    {timeMissionPairs.map(({ time, mission }) => (
                                        <AutoScheduleMissionTableRow
                                            key={time + mission.id}
                                            day={currentDayOfTheWeek}
                                            time={time}
                                            mission={mission}
                                        />
                                    ))}
                                </>
                            )}
                        </Table.Body>
                    </StyledTable>
                    {timeMissionPairs.length > 1 && (
                        <ShowLessOrMoreButton showMore={showMore} setShowMore={setShowMore} />
                    )}
                </StyledNextAutoMission>
            )}
        </>
    )
}
