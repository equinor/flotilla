import { Button, Icon, Popover, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { allDays, allDaysIndexOfToday, DaysOfWeek, parseAutoScheduledJobIds } from 'models/AutoScheduleFrequency'
import { config } from 'config'
import styled from 'styled-components'
import { convertUTCDateToLocalDate } from 'utils/StringFormatting'
import { MissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'
import { Link } from 'react-router-dom'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { useRef, useState } from 'react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'

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

const StyledMissionInfo = styled.div`
    display: flex;
    align-items: center;
    gap: 10px;
`

const StyledButton = styled(Button)`
    &&:hover {
        background-color: ${tokens.colors.ui.background__medium.hex};
    }
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
    const referenceElement = useRef<HTMLButtonElement>(null)
    const [isOpen, setIsOpen] = useState(false)

    const missionStatusType = selectMissionStatusType(day, time, mission)

    const typographyColor =
        missionStatusType === MissionStatusType.SkippedJob || missionStatusType === MissionStatusType.PastJob
            ? tokens.colors.interactive.disabled__text.hex
            : tokens.colors.interactive.primary__resting.hex

    const handleOpen = () => {
        setIsOpen(true)
    }
    const handleClose = () => {
        setIsOpen(false)
    }

    return (
        <Table.Row key={mission.id + time}>
            <Table.Cell>
                <StyledTableRow>
                    <Typography color={typographyColor}>{`${time.substring(0, 5)}`}</Typography>

                    <StyledMissionInfo>
                        <Typography
                            color={typographyColor}
                            as={Link}
                            to={`${config.FRONTEND_BASE_ROUTE}/missiondefinition?id=${mission.id}`}
                            link
                        >
                            {mission.name}
                        </Typography>

                        {mission.comment && (
                            <>
                                <StyledButton
                                    variant="ghost_icon"
                                    aria-haspopup
                                    aria-expanded={isOpen}
                                    ref={referenceElement}
                                    onClick={handleOpen}
                                >
                                    <Icon name={Icons.Info} color={typographyColor} />
                                </StyledButton>

                                <Popover
                                    open={isOpen}
                                    anchorEl={referenceElement.current}
                                    onClose={handleClose}
                                    placement="top"
                                >
                                    <Popover.Content>
                                        <Typography variant="body_short">{mission.comment}</Typography>
                                    </Popover.Content>
                                </Popover>
                            </>
                        )}
                    </StyledMissionInfo>
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
                                {TranslateText('SkipAutoMission') + ' ' + TranslateText('Mission')}
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
                                {TranslateText('SkipAutoMission')}
                            </Button>
                        </StyledDialogActions>
                    </StyledDialog>
                </StyledTableRow>
            </Table.Cell>
        </Table.Row>
    )
}
