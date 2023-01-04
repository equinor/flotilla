import { Typography } from '@equinor/eds-core-react'
import { MissionControlButtons } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'
import { MissionStatusDisplay } from 'components/Pages/FrontPage/MissionOverview/MissionStatusDisplay'
import { format, differenceInMinutes, addMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

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
    let usedTime = differenceInMinutes(Date.now(), new Date(mission.startTime))
    usedTime = usedTime > 0 ? usedTime : 0

    var remainingTime
    if (mission.endTime && new Date(mission.endTime).getFullYear() !== 1) {
        let missionDuration = differenceInMinutes(new Date(mission.endTime), new Date(mission.startTime))
        let diffNowEndTime = differenceInMinutes(new Date(mission.endTime), Date.now())
        remainingTime = missionDuration > diffNowEndTime ? diffNowEndTime : missionDuration
        remainingTime = remainingTime > 0 ? remainingTime : 0
        remainingTime = remainingTime + ' minutes'
    } else {
        remainingTime = 'Not available'
    }

    return (
        <HeaderSection>
            <TitleSection>
                <Typography variant="h1">{mission.name}</Typography>
                <MissionControlButtons mission={mission} />
            </TitleSection>
            <InfoSection>
                <MissionStatusDisplay status={mission.missionStatus} />
                {HeaderText('Start time: ' + format(new Date(mission.startTime), 'HH:mm'))}
                {HeaderText('Time used: ' + usedTime + ' minutes')}
                {HeaderText('Estimated time remaining: ' + remainingTime)}
                {HeaderText('Robot: ' + mission.robot.name)}
                {HeaderText('Battery level: ' + mission.robot.batteryLevel + '%')}
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
