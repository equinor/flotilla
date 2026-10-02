import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'
import { useNow } from 'hooks/useNow'
import { ContentCard } from 'components/Styles/StyledComponents'
import {
    allDays,
    getAllDaysIndexOfToday,
    getTimeMissionPairsForDay,
    isJobScheduledAt,
} from 'models/AutoScheduleFrequency'
import {
    MissionStatusType,
    selectMissionStatusType,
} from 'pages/FrontPage/AutoScheduleSection/AutoScheduleMissionTableRow'

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
    grid-template-columns: 48px 1fr;
    align-items: center;
    gap: 12px;
    padding: 4px 0;
`

const maxMissionsShown = 3

export const DashboardScheduledMissionsView = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const now = useNow()
    const currentDayOfTheWeek = allDays[getAllDaysIndexOfToday(now)]

    const timeMissionPairs = getTimeMissionPairsForDay(missionDefinitions, currentDayOfTheWeek)
        .filter(({ time, mission }) => isJobScheduledAt(mission, time))
        .slice(0, maxMissionsShown)

    return (
        <ContentCard>
            <SectionTitle>{TranslateText('Next auto scheduled mission for today')}</SectionTitle>
            {timeMissionPairs.length === 0 ? (
                <Typography variant="body_short" color={tokens.colors.text.static_icons__tertiary.hex}>
                    {TranslateText('There are no scheduled missions remaining today')}
                </Typography>
            ) : (
                <ScheduleList>
                    {timeMissionPairs.map(({ time, mission }) => {
                        const statusType = selectMissionStatusType(currentDayOfTheWeek, time, mission, now)
                        const color =
                            statusType === MissionStatusType.SkippedJob || statusType === MissionStatusType.PastJob
                                ? tokens.colors.interactive.disabled__text.hex
                                : tokens.colors.interactive.primary__resting.hex

                        return (
                            <ScheduleRow key={mission.id + time}>
                                <Typography color={color}>{time.substring(0, 5)}</Typography>
                                <Typography color={color}>{mission.name}</Typography>
                            </ScheduleRow>
                        )
                    })}
                </ScheduleList>
            )}
        </ContentCard>
    )
}
