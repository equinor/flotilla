import { Button, Icon, Table, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { upcomingOccurrences } from 'models/AutoScheduleOccurrence'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'
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
    width: 100%;
    margin-top: 10px;
    border-top: 1px solid ${tokens.colors.ui.background__medium.hex};
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
    const { autoScheduleOccurrences, autoScheduleLoading, autoScheduleError } = useMissionDefinitionsContext()
    const [showMore, setShowMore] = useState(false)

    const now = useNow()
    const occurrences = upcomingOccurrences(autoScheduleOccurrences, now)

    return (
        <>
            <StyledNextAutoMission>
                <SectionTitle>{TranslateText('Next planned missions - next 24 hours')}</SectionTitle>
                {autoScheduleError || autoScheduleLoading || occurrences.length === 0 ? (
                    <Typography>
                        {TranslateText(
                            autoScheduleError
                                ? 'Failed to retrieve scheduled missions'
                                : autoScheduleLoading
                                  ? 'Loading scheduled missions'
                                  : 'There are no scheduled missions in the next 24 hours'
                        )}
                    </Typography>
                ) : (
                    <StyledTable>
                        <Table.Body style={{ backgroundColor: tokens.colors.ui.background__default.hex }}>
                            {!showMore ? (
                                <AutoScheduleMissionTableRow occurrence={occurrences[0]} />
                            ) : (
                                <>
                                    {occurrences.map((occurrence) => (
                                        <AutoScheduleMissionTableRow
                                            key={occurrence.scheduleKey}
                                            occurrence={occurrence}
                                        />
                                    ))}
                                </>
                            )}
                        </Table.Body>
                    </StyledTable>
                )}
                {occurrences.length > 1 && <ShowLessOrMoreButton showMore={showMore} setShowMore={setShowMore} />}
            </StyledNextAutoMission>
        </>
    )
}
