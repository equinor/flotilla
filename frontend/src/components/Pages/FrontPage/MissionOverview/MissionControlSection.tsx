import { Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotCard, RobotCardPlaceholder } from './RobotCard'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { tokens } from '@equinor/eds-tokens'
import { OngoingMissionCard, OngoingMissionPlaceholderCard } from './OngoingMissionCard'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { MissionHistoryButton } from './MissionHistoryButton'

const MissionControlStyle = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`

const MissionControlBody = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 16px;
`

const MissionControlHeader = styled.div`
    display: grid;
    grid-direction: column;
    gap: 0.5rem;
`

const MissionControlCard = styled(Card)`
    display: flex;
    flex-direction: row;
    gap: 0px;
    width: 960px;
`

export const MissionControlSection = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useRobotContext()
    const { ongoingMissions } = useMissionsContext()

    const missionControlCards = enabledRobots.map((robot, index) => {
        const ongoingMission = ongoingMissions.find((mission) => mission.robot.id === robot.id)
        return (
            <MissionControlCard key={index} style={{ boxShadow: tokens.elevation.raised }}>
                <RobotCard robot={robot} />
                {ongoingMission ? <OngoingMissionCard mission={ongoingMission} /> : <OngoingMissionPlaceholderCard />}
            </MissionControlCard>
        )
    })

    const missionControlPlaceholderCard = (
        <MissionControlCard style={{ boxShadow: tokens.elevation.raised }}>
            <RobotCardPlaceholder />
            <OngoingMissionPlaceholderCard />
        </MissionControlCard>
    )

    return (
        <MissionControlStyle>
            <MissionControlHeader>
                <Typography variant="h1" color="resting">
                    {TranslateText('Mission Control')}
                </Typography>
            </MissionControlHeader>
            <MissionControlBody>
                {enabledRobots.length > 0 && missionControlCards}
                {enabledRobots.length === 0 && missionControlPlaceholderCard}
            </MissionControlBody>
            <MissionHistoryButton />
        </MissionControlStyle>
    )
}
