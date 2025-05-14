import { Button, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { allDays, allDaysIndexOfToday, DaysOfWeek, parseAutoScheduledJobIds } from 'models/AutoScheduleFrequency'
import { config } from 'config'
import styled from 'styled-components'
import { convertUTCDateToLocalDate } from 'utils/StringFormatting'
import { MissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'
import { Link } from 'react-router-dom'

const StyledTableRow = styled.div`
    display: grid;
    align-items: center;
    gap: 1rem;
    grid-template-columns: 100px auto 100px;
`

const skipAutoScheduledMission = async (missionId: string, timeOfDay: string) => {
    await BackendAPICaller.skipAutoScheduledMission(missionId, timeOfDay)
}

export const AutoScheduleMissionTableRow = ({
    day,
    time,
    mission,
}: {
    day: DaysOfWeek
    time: string
    mission: MissionDefinition
}) => {
    const { TranslateText } = useLanguageContext()

    enum MissionStatusType {
        ScheduledJob = 'ScheduledJob',
        SkippedJob = 'SkippedJob',
        PastJob = 'PastJob',
        FutureUnstartedJob = 'FutureUnstartedJob',
    }

    const currentDayOfTheWeek = allDays[allDaysIndexOfToday]

    const selectMissionStatusType = () => {
        if (day !== currentDayOfTheWeek) {
            return MissionStatusType.FutureUnstartedJob
        }
        if (time < convertUTCDateToLocalDate(new Date()).toISOString().substring(11, 19)) {
            return MissionStatusType.PastJob
        }
        if (
            mission.autoScheduleFrequency &&
            mission.autoScheduleFrequency.autoScheduledJobs &&
            parseAutoScheduledJobIds(mission.autoScheduleFrequency.autoScheduledJobs)[time]
        ) {
            return MissionStatusType.ScheduledJob
        }
        return MissionStatusType.SkippedJob
    }

    const missionStatusType = selectMissionStatusType()

    const typographyColor =
        missionStatusType === MissionStatusType.SkippedJob || missionStatusType === MissionStatusType.PastJob
            ? 'disabled'
            : 'primary'

    return (
        <Table.Row key={mission.id + time}>
            <Table.Cell>
                <StyledTableRow>
                    <Typography color={typographyColor}>{`${time.substring(0, 5)}`}</Typography>
                    <Typography
                        color={typographyColor}
                        as={Link}
                        to={`${config.FRONTEND_BASE_ROUTE}/mission-definition/${mission.id}`}
                        link
                    >
                        {mission.name}
                    </Typography>
                    {(missionStatusType === MissionStatusType.ScheduledJob ||
                        missionStatusType === MissionStatusType.SkippedJob) && (
                        <Button
                            style={{ maxWidth: '100px' }}
                            variant="ghost"
                            disabled={missionStatusType === MissionStatusType.SkippedJob}
                            onClick={() => skipAutoScheduledMission(mission.id, time)}
                        >
                            {missionStatusType === MissionStatusType.ScheduledJob
                                ? TranslateText('SkipAutoMission')
                                : TranslateText('Skipped')}
                        </Button>
                    )}
                </StyledTableRow>
            </Table.Cell>
        </Table.Row>
    )
}
