import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotStatusCard'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'

const RobotCardSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`
const RobotView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`

export const RobotStatusSection = () => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useRobotContext()
    const { ongoingMissions } = useMissionsContext()

    const relevantRobots = enabledRobots.sort((robot, robotToCompareWith) =>
        robot.status! !== robotToCompareWith.status!
            ? robot.status! > robotToCompareWith.status!
                ? 1
                : -1
            : robot.name! > robotToCompareWith.name!
              ? 1
              : -1
    )

    const robotDisplay = relevantRobots.map((robot) => (
        <RobotStatusCard
            key={robot.id}
            robot={robot}
            mission={ongoingMissions.find((mission) => mission.robot.id === robot.id)}
        />
    ))

    return (
        <RobotView>
            <Typography color="resting" variant="h1">
                {TranslateText('Robot Status')}
            </Typography>
            <RobotCardSection>
                {relevantRobots.length > 0 && robotDisplay}
                {relevantRobots.length === 0 && <RobotStatusCardPlaceholder />}
            </RobotCardSection>
        </RobotView>
    )
}
