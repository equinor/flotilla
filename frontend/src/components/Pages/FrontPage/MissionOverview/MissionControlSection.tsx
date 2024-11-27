import { Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotCard, RobotCardPlaceholder } from './RobotCard'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { tokens } from '@equinor/eds-tokens'
import { OngoingMissionCard, OngoingMissionPlaceholderCard } from './OngoingMissionCard'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { MissionHistoryButton } from './MissionHistoryButton'
import { Robot } from 'models/Robot'
import { RobotMissionQueueView } from './MissionQueueView'

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

const MissionControlCardStyle = styled(Card)`
    display: flex;
    gap: 0px;
    flex-direction: column;

    @media (min-width: 960px) {
        width: 960px;
    }

    @media (max-width: 960px) {
        max-width: 669px;
        align-self: stretch;
    }
`

const OngoingMissionControlCardStyle = styled.div`
    display: flex;
    gap: 0px;
    align-items: flex-start;

    @media (min-width: 960px) {
        flex-direction: row;
    }

    @media (max-width: 960px) {
        flex-direction: column;
    }
`

export const MissionControlSection = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useRobotContext()

    const missionControlCards = enabledRobots.map((robot, index) => {
        return <MissionControlCard key={index} robot={robot} />
    })

    return (
        <MissionControlStyle>
            <MissionControlHeader>
                <Typography variant="h1" color="resting">
                    {TranslateText('Mission Control')}
                </Typography>
            </MissionControlHeader>
            <MissionControlBody>
                {enabledRobots.length > 0 && missionControlCards}
                {enabledRobots.length === 0 && <MissionControlPlaceholderCard />}
            </MissionControlBody>
            <MissionHistoryButton />
        </MissionControlStyle>
    )
}

const MissionControlCard = ({ robot }: { robot: Robot }) => {
    const { ongoingMissions } = useMissionsContext()
    const ongoingMission = ongoingMissions.find((mission) => mission.robot.id === robot.id)
    return (
        <MissionControlCardStyle
            id={FrontPageSectionId.RobotCard + robot.id}
            style={{ boxShadow: tokens.elevation.raised }}
        >
            <OngoingMissionControlCardStyle>
                <RobotCard robot={robot} />
                {ongoingMission ? <OngoingMissionCard mission={ongoingMission} /> : <OngoingMissionPlaceholderCard />}
            </OngoingMissionControlCardStyle>
            <RobotMissionQueueView robot={robot} />
        </MissionControlCardStyle>
    )
}

const MissionControlPlaceholderCard = () => {
    return (
        <MissionControlCardStyle style={{ boxShadow: tokens.elevation.raised }}>
            <OngoingMissionControlCardStyle>
                <RobotCardPlaceholder />
                <OngoingMissionPlaceholderCard />
            </OngoingMissionControlCardStyle>
        </MissionControlCardStyle>
    )
}
