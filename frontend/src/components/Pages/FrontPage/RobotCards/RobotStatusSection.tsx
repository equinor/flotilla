import { Typography } from '@equinor/eds-core-react'
import { Robot } from 'models/Robot'
import { useEffect } from 'react'
import styled from 'styled-components'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotStatusCard'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useSafeZoneContext } from 'components/Contexts/SafeZoneContext'
import { useRobotContext } from 'components/Contexts/RobotContext'

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
    const { installationCode } = useInstallationContext()
    const { enabledRobots } = useRobotContext()
    const { switchSafeZoneStatus } = useSafeZoneContext()

    const relevantRobots = enabledRobots
        .filter(
            (robot) =>
                robot.currentInstallation.installationCode.toLocaleLowerCase() === installationCode.toLocaleLowerCase()
        )
        .sort((robot, robotToCompareWith) =>
            robot.status! !== robotToCompareWith.status!
                ? robot.status! > robotToCompareWith.status!
                    ? 1
                    : -1
                : robot.name! > robotToCompareWith.name!
                  ? 1
                  : -1
        )

    useEffect(() => {
        const missionQueueFrozenStatus = relevantRobots.some((robot: Robot) => robot.missionQueueFrozen)
        switchSafeZoneStatus(missionQueueFrozenStatus)
    }, [relevantRobots, switchSafeZoneStatus])

    const robotDisplay = relevantRobots.map((robot) => <RobotStatusCard key={robot.id} robot={robot} />)

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
