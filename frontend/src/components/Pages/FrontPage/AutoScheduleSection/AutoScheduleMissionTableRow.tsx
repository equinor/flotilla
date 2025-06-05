import { Button, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { allDays, allDaysIndexOfToday, DaysOfWeek, parseAutoScheduledJobIds } from 'models/AutoScheduleFrequency'
import { config } from 'config'
import styled from 'styled-components'
import { convertUTCDateToLocalDate } from 'utils/StringFormatting'
import { MissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'
import { Link } from 'react-router-dom'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { useState } from 'react'

const StyledTableRow = styled.div`
    display: grid;
    align-items: center;
    gap: 1rem;
    grid-template-columns: 100px auto 100px;
`

const StyledDialogActions = styled(StyledDialog.Actions)`
    display: flex;
    gap: 5px;
`

export const skipAutoScheduledMission = async (missionId: string, timeOfDay: string) => {
    await BackendAPICaller.skipAutoScheduledMission(missionId, timeOfDay)
}

export enum MissionStatusType {
    ScheduledJob = 'ScheduledJob',
    SkippedJob = 'SkippedJob',
    PastJob = 'PastJob',
    FutureUnstartedJob = 'FutureUnstartedJob',
}

const currentDayOfTheWeek = allDays[allDaysIndexOfToday]

const getNowAsTimeOnly = () => {
    return convertUTCDateToLocalDate(new Date()).toISOString().substring(11, 19)
}

export const selectMissionStatusType = (day: DaysOfWeek, time: string, mission: MissionDefinition) => {
    if (day !== currentDayOfTheWeek) {
        return MissionStatusType.FutureUnstartedJob
    }
    if (time < getNowAsTimeOnly()) {
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
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)

    const missionStatusType = selectMissionStatusType(day, time, mission)

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
                            onClick={() => setIsDialogOpen(true)}
                        >
                            {missionStatusType === MissionStatusType.ScheduledJob
                                ? TranslateText('SkipAutoMission')
                                : TranslateText('Skipped')}
                        </Button>
                    )}
                    <StyledDialog open={isDialogOpen}>
                        <StyledDialog.Header>
                            <StyledDialog.Title>
                                {TranslateText('Skip') + ' ' + TranslateText('Mission')}
                            </StyledDialog.Title>
                        </StyledDialog.Header>
                        <StyledDialog.CustomContent>
                            <Typography>
                                {TranslateText('Are you sure you want to skip {0} scheduled for today at {1}?', [
                                    mission.name,
                                    time.slice(0, 5),
                                ])}
                            </Typography>
                        </StyledDialog.CustomContent>
                        <StyledDialogActions>
                            <Button onClick={() => setIsDialogOpen(false)} variant="outlined" color="primary">
                                {TranslateText('Close')}
                            </Button>
                            <Button
                                onClick={() => skipAutoScheduledMission(mission.id, time)}
                                variant="outlined"
                                color="danger"
                            >
                                {TranslateText('Skip')}
                            </Button>
                        </StyledDialogActions>
                    </StyledDialog>
                </StyledTableRow>
            </Table.Cell>
        </Table.Row>
    )
}
