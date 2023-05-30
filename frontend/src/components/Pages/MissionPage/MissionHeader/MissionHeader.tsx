import { Typography } from '@equinor/eds-core-react'
import { MissionControlButtons } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'
import { MissionStatusDisplay } from 'components/Pages/FrontPage/MissionOverview/MissionStatusDisplay'
import { format, differenceInMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { StatusReason } from '../StatusReason'
import { MissionRestartButton } from 'components/Pages/FrontPage/MissionOverview/MissionRestartButton'

const HeaderSection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
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

interface MissionHeaderProps {
    mission: Mission
}

export function MissionHeader({ mission }: MissionHeaderProps) {
    const barToMillibar = 1000
    const isMissionCompleted = mission.endTime ? true : false
    var { startTime, startDate, usedTime, remainingTime } = StartUsedAndRemainingTime(mission)
    var missionIsActive = false
    if (mission.status === MissionStatus.Ongoing || mission.status === MissionStatus.Paused) {
        missionIsActive = true
    }

    return (
        <HeaderSection>
            <TitleSection>
                <Typography variant="h1">{mission.name}</Typography>
                {missionIsActive && <MissionControlButtons mission={mission} />}
                {mission.isCompleted && <MissionRestartButton mission={mission} />}
            </TitleSection>
            <Typography
                variant="body_long_italic"
                group="paragraph"
                color={tokens.colors.text.static_icons__secondary.rgba}
            >
                {mission.description && TranslateText('Description') + ': ' + mission.description}
            </Typography>
            <StatusReason mission={mission}></StatusReason>
            <InfoSection>
                <MissionStatusDisplay status={mission.status} />
                {HeaderText(TranslateText('Start date') + ': ' + startDate)}
                {HeaderText(TranslateText('Start time') + ': ' + startTime)}
                {HeaderText(TranslateText('Time used') + ': ' + usedTime)}
                {!isMissionCompleted && HeaderText(TranslateText('Estimated time remaining') + ': ' + remainingTime)}
                {HeaderText(TranslateText('Robot') + ': ' + mission.robot.name)}
                {!isMissionCompleted &&
                    HeaderText(TranslateText('Battery level') + ': ' + mission.robot.batteryLevel + '%')}
                {!isMissionCompleted &&
                    mission.robot.pressureLevel &&
                    HeaderText(
                        TranslateText('Pressure level') +
                            ': ' +
                            Math.round(mission.robot.pressureLevel * barToMillibar) +
                            'mBar'
                    )}
            </InfoSection>
        </HeaderSection>
    )
}

function HeaderText(text: string) {
    return (
        <Typography variant="body_short" group="paragraph" color={tokens.colors.text.static_icons__secondary.rgba}>
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
            remainingTime = Math.max(estimatedDurationInMinutes - usedTimeInMinutes, 0) + ' ' + TranslateText('minutes')
        else remainingTime = 'N/A'
    } else {
        startTime = 'N/A'
        startDate = 'N/A'
        usedTimeInMinutes = 0
        if (estimatedDurationInMinutes) remainingTime = estimatedDurationInMinutes + ' ' + TranslateText('minutes')
        else remainingTime = 'N/A'
    }
    const usedTime: string = usedTimeInMinutes + ' ' + TranslateText('minutes')
    return { startTime, startDate, usedTime, remainingTime }
}
