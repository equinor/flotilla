import { Card, Typography } from '@equinor/eds-core-react'
import { MissionControlButtons } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'
import { MissionStatusDisplay } from 'components/Pages/FrontPage/MissionOverview/MissionStatusDisplay'
import { format, differenceInMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext, TranslateTextWithContext } from 'components/Contexts/LanguageContext'
import { StatusReason } from '../StatusReason'
import { MissionRestartButton } from 'components/Pages/FrontPage/MissionOverview/MissionRestartButton'

const HeaderSection = styled(Card)`
    width: 100%;
    min-width: 600px;
    height: auto;
    display: flex;
    top: 60px;
    flex-direction: column;
    gap: 0.4rem;
    position: sticky;
    margin-top: auto;
    background-color: white;
    z-index: 1;
    border-color: white;
`
const TitleSection = styled.div`
    display: flex;
    align-items: center;
    gap: 10px;
`
const InfoSection = styled.div`
    display: flex;
    align-content: start;
    align-items: flex-end;
    gap: 1.2rem;
`

function HeaderText(text: string) {
    return (
        <Typography variant="body_short" group="paragraph" color={tokens.colors.text.static_icons__secondary.hex}>
            {text}
        </Typography>
    )
}

function StartUsedAndRemainingTime(mission: Mission): {
    startTime: string
    startDate: string
    usedTime: string
    remainingTime: string
} {
    var startTime: string
    var startDate: string
    var remainingTime: string
    var usedTimeInMinutes: number
    var estimatedDurationInMinutes: number | undefined
    const translatedMinutes = TranslateTextWithContext('minutes')
    if (mission.estimatedDuration) {
        // Convert from seconds to minutes, rounding up
        estimatedDurationInMinutes = Math.ceil(mission.estimatedDuration / 60)
    }

    if (mission.endTime) {
        startTime = mission.startTime
            ? format(new Date(mission.startTime), 'HH:mm')
            : format(new Date(mission.endTime), 'HH:mm')
        startDate = mission.startTime
            ? format(new Date(mission.startTime), 'dd/MM/yyy')
            : format(new Date(mission.endTime), 'dd/MM/yyy')
        usedTimeInMinutes = mission.startTime
            ? differenceInMinutes(new Date(mission.endTime), new Date(mission.startTime))
            : 0
        remainingTime = 'N/A'
    } else if (mission.startTime) {
        startTime = format(new Date(mission.startTime), 'HH:mm')
        startDate = format(new Date(mission.startTime), 'dd/MM/yyy')
        usedTimeInMinutes = differenceInMinutes(Date.now(), new Date(mission.startTime))
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

interface MissionHeaderProps {
    mission: Mission
}

export function MissionHeader({ mission }: MissionHeaderProps) {
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

    const { startTime, startDate, usedTime, remainingTime } = StartUsedAndRemainingTime(mission)
    const isMissionActive = mission.status === MissionStatus.Ongoing || mission.status === MissionStatus.Paused

    return (
        <HeaderSection>
            <TitleSection>
                <Typography variant="h1">{mission.name}</Typography>
                {isMissionActive && <MissionControlButtons mission={mission} />}
                {mission.isCompleted && <MissionRestartButton mission={mission} />}
            </TitleSection>
            <Typography
                variant="body_long_italic"
                group="paragraph"
                color={tokens.colors.text.static_icons__secondary.hex}
            >
                {mission.description && `${translatedDescription}: ${mission.description}`}
            </Typography>
            <StatusReason mission={mission}></StatusReason>
            <InfoSection>
                <MissionStatusDisplay status={mission.status} />
                {HeaderText(`${translatedStartDate}: ${startDate}`)}
                {HeaderText(`${translatedStartTime}: ${startTime}`)}
                {HeaderText(`${translatedUsedTime}: ${usedTime}`)}
                {!isMissionCompleted && HeaderText(`${translatedEstimatedTimeRemaining}: ${remainingTime}`)}
                {HeaderText(`${translatedRobot}: ${mission.robot.name}`)}
                {!isMissionCompleted && HeaderText(`${translatedBatteryLevel}: ${mission.robot.batteryLevel}%`)}
                {!isMissionCompleted &&
                    mission.robot.pressureLevel &&
                    HeaderText(
                        `${translatedPressureLevel}: ${Math.round(mission.robot.pressureLevel * barToMillibar)}mBar`
                    )}
            </InfoSection>
        </HeaderSection>
    )
}
