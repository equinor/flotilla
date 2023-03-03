import { Typography } from '@equinor/eds-core-react'
import { MissionControlButtons } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'
import { MissionStatusDisplay } from 'components/Pages/FrontPage/MissionOverview/MissionStatusDisplay'
import { format, differenceInMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'

const HeaderSection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1.2rem;
`
const TitleSection = styled.div`
    display: flex;
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
    var { startTime, usedTime, remainingTime } = StartUsedAndRemainingTime(mission)
    var showControlButtons = false
    if (mission.missionStatus === MissionStatus.Ongoing || mission.missionStatus === MissionStatus.Paused) {
        showControlButtons = true
    }

    return (
        <HeaderSection>
            <TitleSection>
                <Typography variant="h1">{mission.name}</Typography>
                {showControlButtons && <MissionControlButtons mission={mission} />}
            </TitleSection>
            <InfoSection>
                <MissionStatusDisplay status={mission.missionStatus} />
                {HeaderText(Text('Start time') + ': ' + startTime)}
                {HeaderText(Text('Time used') + ': ' + usedTime)}
                {HeaderText(Text('Estimated time remaining') + ': ' + remainingTime)}
                {HeaderText(Text('Robot') + ': ' + mission.robot.name)}
                {HeaderText(Text('Battery level') + ': ' + mission.robot.batteryLevel + '%')}
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

function StartUsedAndRemainingTime(mission: Mission) {
    var startTime
    var remainingTime
    var usedTime

    let dateTime = mission.estimatedDuration.split('.')
    const days = dateTime.length === 1 ? 0 : dateTime[0].split(':')[0]
    const time = dateTime.length === 1 ? dateTime[0].split(':') : dateTime[1].split(':')
    const estimatedDuration = +time[1] + 60 * (+time[0] + +days * 24)

    if (mission.endTime) {
        startTime = mission.startTime
            ? format(new Date(mission.startTime), 'HH:mm')
            : format(new Date(mission.endTime), 'HH:mm')
        usedTime = mission.startTime ? differenceInMinutes(new Date(mission.endTime), new Date(mission.startTime)) : 0
        remainingTime = 'N/A'
    } else if (mission.startTime) {
        startTime = format(new Date(mission.startTime), 'HH:mm')
        usedTime = differenceInMinutes(Date.now(), new Date(mission.startTime))
        remainingTime = Math.max(estimatedDuration - usedTime, 0) + ' ' + Text('minutes')
    } else {
        startTime = 'N/A'
        usedTime = 0
        remainingTime = estimatedDuration + ' ' + Text('minutes')
    }
    usedTime = usedTime + ' ' + Text('minutes')
    return { startTime, usedTime, remainingTime }
}
