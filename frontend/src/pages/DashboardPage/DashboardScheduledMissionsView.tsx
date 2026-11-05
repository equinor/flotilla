import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'
import { useNow } from 'hooks/useNow'
import { ContentCard } from 'components/Styles/StyledComponents'
import { formatScheduledAt, upcomingScheduledOccurrences } from 'models/AutoScheduleOccurrence'

const SectionTitle = styled.p`
    margin: 0 0 10px 0;
    font-family: Equinor, sans-serif;
    font-size: 0.92rem;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__default.hex};
`

const ScheduleList = styled.div`
    display: flex;
    flex-direction: column;
    gap: 4px;
    max-height: calc(25vh / var(--dashboard-zoom, 1));
    overflow-y: auto;
`
const ScheduleRow = styled.div`
    display: grid;
    grid-template-columns: 110px 1fr;
    align-items: center;
    gap: 12px;
    padding: 4px 0;
`

export const DashboardScheduledMissionsView = () => {
    const { TranslateText, language } = useLanguageContext()
    const { autoScheduleOccurrences, autoScheduleLoading, autoScheduleError } = useMissionDefinitionsContext()
    const now = useNow()
    const occurrences = upcomingScheduledOccurrences(autoScheduleOccurrences, now)

    return (
        <ContentCard>
            <SectionTitle>{TranslateText('Next planned missions - next 24 hours')}</SectionTitle>
            {autoScheduleError || autoScheduleLoading || occurrences.length === 0 ? (
                <Typography variant="body_short" color={tokens.colors.text.static_icons__tertiary.hex}>
                    {TranslateText(
                        autoScheduleError
                            ? 'Failed to retrieve scheduled missions'
                            : autoScheduleLoading
                              ? 'Loading scheduled missions'
                              : 'There are no scheduled missions in the next 24 hours'
                    )}
                </Typography>
            ) : (
                <ScheduleList>
                    {occurrences.map((occurrence) => {
                        return (
                            <ScheduleRow key={occurrence.scheduleKey}>
                                <Typography>{formatScheduledAt(occurrence.scheduledAt, language)}</Typography>
                                <Typography>{occurrence.mission.name}</Typography>
                            </ScheduleRow>
                        )
                    })}
                </ScheduleList>
            )}
        </ContentCard>
    )
}
