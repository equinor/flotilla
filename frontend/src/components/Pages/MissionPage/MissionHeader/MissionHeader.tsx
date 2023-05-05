import { Typography } from '@equinor/eds-core-react'
import { MissionControlButtons } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'
import { MissionStatusDisplay } from 'components/Pages/FrontPage/MissionOverview/MissionStatusDisplay'
import { format, differenceInMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'
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
    var { startTime, usedTime, remainingTime } = StartUsedAndRemainingTime(mission)
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
                {mission.description && Text('Description') + ': ' + mission.description}
            </Typography>
            <StatusReason mission={mission}></StatusReason>
            <InfoSection>
                <MissionStatusDisplay status={mission.status} />
                {HeaderText(Text('Start time') + ': ' + startTime)}
                {HeaderText(Text('Time used') + ': ' + usedTime)}
                {!isMissionCompleted && HeaderText(Text('Estimated time remaining') + ': ' + remainingTime)}
                {HeaderText(Text('Robot') + ': ' + mission.robot.name)}
                {!isMissionCompleted && HeaderText(Text('Battery level') + ': ' + mission.robot.batteryLevel + '%')}
                {!isMissionCompleted &&
                    mission.robot.pressureLevel &&
                    HeaderText(
                        Text('Pressure level') + ': ' + Math.round(mission.robot.pressureLevel * barToMillibar) + 'mBar'
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

function StartUsedAndRemainingTime(mission: Mission): { startTime: string; usedTime: string; remainingTime: string } {
    var startTime: string
    var remainingTime: string
    var usedTimeInMinutes: number
    var estimatedDuration: number | undefined

    if (mission.estimatedDuration) {
        let dateTime = mission.estimatedDuration.split('.')
        const days = dateTime.length === 1 ? 0 : dateTime[0].split(':')[0]
        const time = dateTime.length === 1 ? dateTime[0].split(':') : dateTime[1].split(':')
        estimatedDuration = +time[1] + 60 * (+time[0] + +days * 24)
    }

    if (mission.endTime) {
        startTime = mission.startTime
            ? format(new Date(mission.startTime), 'HH:mm')
            : format(new Date(mission.endTime), 'HH:mm')
        usedTimeInMinutes = mission.startTime
            ? differenceInMinutes(new Date(mission.endTime), new Date(mission.startTime))
            : 0
        remainingTime = 'N/A'
    } else if (mission.startTime) {
        startTime = format(new Date(mission.startTime), 'HH:mm')
        usedTimeInMinutes = differenceInMinutes(Date.now(), new Date(mission.startTime))
        if (estimatedDuration)
            remainingTime = Math.max(estimatedDuration - usedTimeInMinutes, 0) + ' ' + Text('minutes')
        else remainingTime = 'N/A'
    } else {
        startTime = 'N/A'
        usedTimeInMinutes = 0
        if (estimatedDuration) remainingTime = estimatedDuration + ' ' + Text('minutes')
        else remainingTime = 'N/A'
    }
    const usedTime: string = usedTimeInMinutes + ' ' + Text('minutes')
    return { startTime, usedTime, remainingTime }
}
