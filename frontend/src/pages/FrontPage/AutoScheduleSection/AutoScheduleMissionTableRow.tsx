import { Button, Icon, Popover, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { allDays, allDaysIndexOfToday, DaysOfWeek, parseAutoScheduledJobIds } from 'models/AutoScheduleFrequency'
import styled from 'styled-components'
import { convertUTCDateToLocalDate } from 'utils/StringFormatting'
import { MissionDefinition } from 'models/MissionDefinition'
import { Link } from 'react-router-dom'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { useContext, useRef, useState } from 'react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { MissionSchedulingEditDialog } from 'components/Dialogs/MissionEditDialog'

const StyledTableRow = styled.div`
    display: grid;
    align-items: center;
    gap: 1rem;
    grid-template-columns: 100px auto 16px 100px;
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

const CalenderButton = styled(Button)`
    max-width: 100px;
`

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
    const backendApi = useBackendApi()
    const { installation } = useContext(InstallationContext)
    const [isSkipDialogOpen, setIsSkipDialogOpen] = useState<boolean>(false)
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
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

    const handleAutoScheduleSkip = () => {
        backendApi.skipAutoScheduledMission(mission.id, time)
        setIsSkipDialogOpen(false)
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
                            to={`/${installation.installationCode}/missiondefinition/${mission.id}`}
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
                    <CalenderButton variant="ghost_icon" onClick={() => setIsEditDialogOpen(true)}>
                        <Icon name={Icons.Edit} size={16} color={typographyColor} />
                    </CalenderButton>
                    {(missionStatusType === MissionStatusType.ScheduledJob ||
                        missionStatusType === MissionStatusType.SkippedJob) && (
                        <CalenderButton
                            variant="ghost"
                            disabled={missionStatusType === MissionStatusType.SkippedJob}
                            onClick={() => setIsSkipDialogOpen(true)}
                        >
                            {missionStatusType === MissionStatusType.ScheduledJob
                                ? TranslateText('SkipAutoMission')
                                : TranslateText('Skipped')}
                        </CalenderButton>
                    )}
                    <StyledDialog open={isSkipDialogOpen}>
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
                            <Button onClick={() => setIsSkipDialogOpen(false)} variant="outlined" color="primary">
                                {TranslateText('Close')}
                            </Button>
                            <Button onClick={handleAutoScheduleSkip} variant="outlined" color="danger">
                                {TranslateText('SkipAutoMission')}
                            </Button>
                        </StyledDialogActions>
                    </StyledDialog>
                    <MissionSchedulingEditDialog
                        mission={mission}
                        isOpen={isEditDialogOpen}
                        onClose={() => setIsEditDialogOpen(false)}
                    />
                </StyledTableRow>
            </Table.Cell>
        </Table.Row>
    )
}
