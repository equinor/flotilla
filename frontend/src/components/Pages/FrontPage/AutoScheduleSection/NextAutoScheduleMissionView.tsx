import { Button, Icon, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledTableBody } from 'components/Styles/StyledComponents'
import { allDays, allDaysIndexOfToday, parseAutoScheduledJobIds } from 'models/AutoScheduleFrequency'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { AutoScheduleMissionTableRow } from './AutoScheduleMissionTableRow'

const StyledNextAutoMission = styled.div`
    margin-top: 30px;
`

const StyledTable = styled(Table)`
    width: 960px;
    margin-top: 10px;
    border-top: 1px solid #dcdcdc;

    @media (max-width: 960px) {
        width: 95%;
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

    const autoScheduleMissionDefinitions = missionDefinitions.filter((m) => m.autoScheduleFrequency)
    const currentDayOfTheWeek = allDays[allDaysIndexOfToday]

    const missionDefinitionList = autoScheduleMissionDefinitions.filter((m) =>
        m.autoScheduleFrequency!.daysOfWeek.includes(currentDayOfTheWeek)
    )

    const timeMissionPairs = missionDefinitionList
        .filter((m) => m.autoScheduleFrequency?.autoScheduledJobs)
        .flatMap((m) =>
            m
                .autoScheduleFrequency!.timesOfDayCET.filter(
                    (time) => parseAutoScheduledJobIds(m.autoScheduleFrequency!.autoScheduledJobs!)[time]
                )
                .map((time) => ({ time, mission: m }))
        )
        .sort((a, b) => (a.time > b.time ? 1 : -1))

    return (
        <>
            {timeMissionPairs.length > 0 && (
                <StyledNextAutoMission>
                    <Typography variant="h5">{TranslateText('Next auto scheduled mission for today')}</Typography>
                    <StyledTable>
                        <StyledTableBody>
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
                        </StyledTableBody>
                    </StyledTable>
                    {timeMissionPairs.length > 1 && (
                        <ShowLessOrMoreButton showMore={showMore} setShowMore={setShowMore} />
                    )}
                </StyledNextAutoMission>
            )}
        </>
    )
}
