import { Button, Card, Typography } from '@equinor/eds-core-react'
import { MissionControlButtons } from 'components/Displays/MissionButtons/MissionControlButtons'
import { MissionStatusDisplay } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { differenceInMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StatusReason } from '../StatusReason'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { TaskStatus } from 'models/Task'
import { convertUTCDateToLocalDate, formatDateTime } from 'utils/StringFormatting'
import { calculateRemaindingTimeInMinutes } from 'utils/CalculateRemaingingTime'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { phone_width } from 'utils/constants'

const HeaderSection = styled(Card)`
    width: 100%;
    padding: 15px 0px 15px 0px;
    top: 60px;
    position: sticky;
    z-index: 1;
    box-shadow: none;
    background-color: ${tokens.colors.ui.background__light.hex};
`
const TitleSection = styled.div`
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 20px;
`
const InfoSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 32px;
    width: fit-content;
    @media (max-width: ${phone_width}) {
        display: grid;
        grid-template-columns: repeat(3, calc(75vw / 3));
        gap: 32px;
        width: fit-content;
        align-items: end;
    }
`
const StyledTitleText = styled.div`
    display: grid;
    grid-direction: column;
    gap: 5px;
`
const StyledTypography = styled(Typography)`
    font-family: Equinor;
    font-size: 32px;
    font-style: normal;
    font-weight: 400;
    line-height: 40px;
    @media (max-width: ${phone_width}) {
        font-size: 24px;
    }
`

const StyledMissionHeader = styled(Card)`
    display: flex;
    padding: 10px;
    flex-direction: column;
    align-items: flex-start;
    align-content: center;
    gap: 24px;
    flex: 1 0 0;
    align-self: stretch;
    border-radius: 6px;
    max-height: fit-content;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    background: ${tokens.colors.ui.background__default.hex};
`

const HeaderText = (title: string, text: string) => {
    return (
        <StyledTitleText>
            <Typography variant="meta" color={tokens.colors.text.static_icons__secondary.hex}>
                {title}
            </Typography>
            <Typography variant="caption" color={tokens.colors.text.static_icons__secondary.hex}>
                {text}
            </Typography>
        </StyledTitleText>
    )
}

const getStartUsedAndRemainingTime = (
    mission: Mission,
    translatedMinutes: string
): {
    startTime: string
    startDate: string
    usedTime: string
    remainingTime: string
} => {
    let startTime: string
    let startDate: string
    let remainingTime: string
    let usedTimeInMinutes: number

    if (mission.endTime) {
        startTime = mission.startTime
            ? formatDateTime(mission.startTime, 'HH:mm')
            : formatDateTime(mission.endTime, 'HH:mm')

        startDate = mission.startTime
            ? formatDateTime(mission.startTime, 'dd/MM/yyy')
            : formatDateTime(mission.endTime, 'dd/MM/yyy')
        usedTimeInMinutes = mission.startTime
            ? differenceInMinutes(
                  convertUTCDateToLocalDate(mission.endTime),
                  convertUTCDateToLocalDate(mission.startTime)
              )
            : 0
        remainingTime = 'N/A'
    } else if (mission.startTime) {
        startTime = formatDateTime(mission.startTime, 'HH:mm')
        startDate = formatDateTime(mission.startTime, 'dd/MM/yyy')
        usedTimeInMinutes = differenceInMinutes(Date.now(), convertUTCDateToLocalDate(mission.startTime))
        if (mission.estimatedTaskDuration)
            remainingTime =
                calculateRemaindingTimeInMinutes(mission.tasks, mission.estimatedTaskDuration) + ' ' + translatedMinutes
        else remainingTime = 'N/A'
    } else {
        startTime = 'N/A'
        startDate = 'N/A'
        usedTimeInMinutes = 0
        if (mission.estimatedTaskDuration)
            remainingTime =
                calculateRemaindingTimeInMinutes(mission.tasks, mission.estimatedTaskDuration) + ' ' + translatedMinutes
        else remainingTime = 'N/A'
    }
    const usedTime: string = usedTimeInMinutes + ' ' + translatedMinutes
    return { startTime, startDate, usedTime, remainingTime }
}

export const MissionHeader = ({ mission }: { mission: Mission }) => {
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const isMissionCompleted = mission.endTime ? true : false

    const translatedStartDate = TranslateText('Start date')
    const translatedStartTime = TranslateText('Start time')
    const translatedUsedTime = TranslateText('Time used')
    const translatedEstimatedTimeRemaining = TranslateText('Estimated time remaining')
    const translatedRobot = TranslateText('Robot')
    const translatedDescription = TranslateText('Description')
    const translatedTasks = TranslateText('Completed Tasks')
    const translatedStatus = TranslateText('Status')

    const translatedMinutes = TranslateText('minutes')
    const { startTime, startDate, usedTime, remainingTime } = getStartUsedAndRemainingTime(mission, translatedMinutes)
    const isMissionActive = mission.status === MissionStatus.Ongoing || mission.status === MissionStatus.Paused

    const missionHasFailedTasks = mission.tasks.some(
        (t) => t.status !== TaskStatus.PartiallySuccessful && t.status !== TaskStatus.Successful
    )

    const numberOfCompletedTasks = mission.tasks.filter((task) => task.isCompleted).length

    return (
        <>
            <HeaderSection>
                <TitleSection>
                    <StyledTypography>{mission.name}</StyledTypography>
                    {isMissionActive && (
                        <MissionControlButtons
                            isReturnToHomeMission={false}
                            missionName={mission.name}
                            robotId={mission.robot.id}
                            missionStatus={mission.status}
                        />
                    )}
                    {mission.endTime && (
                        <MissionRestartButton
                            mission={mission}
                            hasFailedTasks={missionHasFailedTasks}
                            smallButton={false}
                        />
                    )}
                    <Button
                        variant="outlined"
                        onClick={() => navigate(`${config.FRONTEND_BASE_ROUTE}/missiondefinition-${mission.missionId}`)}
                    >
                        {TranslateText('View mission definition')}
                    </Button>
                </TitleSection>
                <Typography
                    variant="body_long"
                    group="paragraph"
                    color={tokens.colors.text.static_icons__secondary.hex}
                >
                    {mission.description && `${translatedDescription}: ${mission.description}`}
                </Typography>
                <StatusReason statusReason={mission.statusReason} status={mission.status}></StatusReason>
            </HeaderSection>
            <StyledMissionHeader>
                <InfoSection>
                    <div>
                        {HeaderText(translatedStatus, '')}
                        <MissionStatusDisplay status={mission.status} />
                    </div>
                    {HeaderText(translatedTasks, `${numberOfCompletedTasks + '/' + mission.tasks.length}`)}
                    {HeaderText(translatedStartDate, `${startDate}`)}
                    {HeaderText(translatedStartTime, `${startTime}`)}
                    {HeaderText(translatedUsedTime, `${usedTime}`)}
                    {!isMissionCompleted && HeaderText(translatedEstimatedTimeRemaining, `${remainingTime}`)}
                    {HeaderText(translatedRobot, `${mission.robot.name}`)}
                </InfoSection>
            </StyledMissionHeader>
        </>
    )
}
