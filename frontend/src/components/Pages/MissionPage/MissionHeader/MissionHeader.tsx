import { Card, Typography } from '@equinor/eds-core-react'
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

const HeaderSection = styled(Card)`
    width: 100%;
    padding: 15px 0px 15px 0px;
    box-shadow:
        -10px 0px 0px 0px white,
        30px 0px 0px 0px white;
    top: 60px;
    position: sticky;
    z-index: 1;
`

const TitleSection = styled.div`
    display: flex;
    align-items: center;
    gap: 20px;
`
const InfoSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    max-width: 950px;
`
const StyledCard = styled(Card)`
    display: flex;
    flex: 1 0 0;
    padding: 8px 16px;
    flex-direction: row;
    background: ${tokens.colors.ui.background__light.hex};
    gap: 24px;
    align-items: stretch;
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
    line-height: 40px; /* 125% */

    @media (max-width: 500px) {
        font-family: Equinor;
        font-size: 24px;
        font-style: normal;
    }
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
    var startTime: string
    var startDate: string
    var remainingTime: string
    var usedTimeInMinutes: number
    var estimatedDurationInMinutes: number | undefined
    if (mission.estimatedDuration) {
        // Convert from seconds to minutes, rounding up
        estimatedDurationInMinutes = Math.ceil(mission.estimatedDuration / 60)
    }

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
        if (estimatedDurationInMinutes)
            remainingTime = Math.max(estimatedDurationInMinutes - usedTimeInMinutes, 0) + ' ' + translatedMinutes
        else remainingTime = 'N/A'
    } else {
        startTime = 'N/A'
        startDate = 'N/A'
        usedTimeInMinutes = 0
        if (estimatedDurationInMinutes) remainingTime = estimatedDurationInMinutes + ' ' + translatedMinutes
        else remainingTime = 'N/A'
    }
    const usedTime: string = usedTimeInMinutes + ' ' + translatedMinutes
    return { startTime, startDate, usedTime, remainingTime }
}

export const MissionHeader = ({ mission }: { mission: Mission }) => {
    const { TranslateText } = useLanguageContext()
    const barToMillibar = 1000
    const isMissionCompleted = mission.endTime ? true : false

    const translatedStartDate = TranslateText('Start date')
    const translatedStartTime = TranslateText('Start time')
    const translatedUsedTime = TranslateText('Time used')
    const translatedEstimatedTimeRemaining = TranslateText('Estimated time remaining')
    const translatedRobot = TranslateText('Robot')
    const translatedBatteryLevel = TranslateText('Battery level')
    const translatedPressureLevel = TranslateText('Pressure level')
    const translatedDescription = TranslateText('Description')
    const translatedArea = TranslateText('Area')
    const translatedTasks = TranslateText('Completed Tasks')
    const translatedStatus = TranslateText('Status')

    const translatedMinutes = TranslateText('minutes')
    const { startTime, startDate, usedTime, remainingTime } = getStartUsedAndRemainingTime(mission, translatedMinutes)
    const isMissionActive = mission.status === MissionStatus.Ongoing || mission.status === MissionStatus.Paused

    const missionHasFailedTasks = mission.tasks.some(
        (t) => t.status !== TaskStatus.PartiallySuccessful && t.status !== TaskStatus.Successful
    )

    const numberOfCompletedTasks = mission.tasks.filter((task) => task.isCompleted).length

    const batteryValue = mission.robot.batteryLevel ? `${Math.round(mission.robot.batteryLevel)}%` : '---%'

    return (
        <>
            <HeaderSection>
                <TitleSection>
                    <StyledTypography>{mission.name}</StyledTypography>
                    {isMissionActive && (
                        <MissionControlButtons
                            missionName={mission.name}
                            robotId={mission.robot.id}
                            missionStatus={mission.status}
                        />
                    )}
                    {mission.endTime && (
                        <MissionRestartButton mission={mission} hasFailedTasks={missionHasFailedTasks} />
                    )}
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
            <InfoSection>
                <StyledCard>
                    <div>
                        {HeaderText(translatedStatus, '')}
                        <MissionStatusDisplay status={mission.status} />
                    </div>
                    {HeaderText(translatedArea, `${mission.area?.areaName}`)}
                    {HeaderText(translatedTasks, `${numberOfCompletedTasks + '/' + mission.tasks.length}`)}
                </StyledCard>
                <StyledCard>
                    {HeaderText(translatedStartDate, `${startDate}`)}
                    {HeaderText(translatedStartTime, `${startTime}`)}
                </StyledCard>
                <StyledCard>
                    {HeaderText(translatedUsedTime, `${usedTime}`)}
                    {!isMissionCompleted && HeaderText(translatedEstimatedTimeRemaining, `${remainingTime}`)}
                </StyledCard>
                <StyledCard>
                    {HeaderText(translatedRobot, `${mission.robot.name}`)}
                    {!isMissionCompleted && HeaderText(translatedBatteryLevel, batteryValue)}
                    {!isMissionCompleted &&
                        mission.robot.pressureLevel &&
                        HeaderText(
                            translatedPressureLevel,
                            `${Math.round(mission.robot.pressureLevel * barToMillibar)}mBar`
                        )}
                </StyledCard>
            </InfoSection>
        </>
    )
}
