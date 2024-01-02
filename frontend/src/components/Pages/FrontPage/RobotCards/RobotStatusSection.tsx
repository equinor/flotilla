import { Typography } from '@equinor/eds-core-react'
import { Robot } from 'models/Robot'
import { useEffect } from 'react'
import styled from 'styled-components'
import { BlockedRobotAlertContent } from 'components/Alerts/BlockedRobotAlert'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotStatusCard'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
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
const isRobotBlocked = (robot: Robot): boolean => {
    return robot.status === 'Blocked'
}

export const RobotStatusSection = () => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const { enabledRobots } = useRobotContext()
    const { switchSafeZoneStatus } = useSafeZoneContext()
    const { setAlert } = useAlertContext()
    const relevantRobots = enabledRobots
        .filter(
            (robot) =>
                robot.currentInstallation.installationCode.toLocaleLowerCase() === installationCode.toLocaleLowerCase()
        )
        .sort((robot, robotToCompareWith) => (robot.status! > robotToCompareWith.status! ? 1 : -1))

    useEffect(() => {
        const missionQueueFozenStatus = relevantRobots.some((robot: Robot) => robot.missionQueueFrozen)
        switchSafeZoneStatus(missionQueueFozenStatus)
        const blockedRobots = relevantRobots.filter(isRobotBlocked)
        if (blockedRobots.length > 0) {
            setAlert(AlertType.BlockedRobot, <BlockedRobotAlertContent robot={blockedRobots[0]} />)
        }
    }, [enabledRobots, installationCode, switchSafeZoneStatus, relevantRobots, setAlert])

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
