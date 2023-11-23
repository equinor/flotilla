import { Typography } from '@equinor/eds-core-react'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
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
    const [robots, setRobots] = useState<Robot[]>([])

    useEffect(() => {
        const sortRobotsByStatus = (robots: Robot[]): Robot[] => {
            const sortedRobots = robots.sort((robot, robotToCompareWith) =>
                robot.status! > robotToCompareWith.status! ? 1 : -1
            )
            return sortedRobots
        }
        const relevantRobots = sortRobotsByStatus(
            enabledRobots.filter((robot) => {
                return robot.currentInstallation.toLocaleLowerCase() === installationCode.toLocaleLowerCase()
            })
        )
        setRobots(relevantRobots)

        const missionQueueFozenStatus = relevantRobots
            .map((robot: Robot) => {
                return robot.missionQueueFrozen
            })
            .filter((status) => status === true)

        if (missionQueueFozenStatus.length > 0) switchSafeZoneStatus(true)
        else switchSafeZoneStatus(false)
    }, [enabledRobots, installationCode, switchSafeZoneStatus])

    const getRobotDisplay = () => {
        return robots.map((robot) => <RobotStatusCard key={robot.id} robot={robot} />)
    }

    return (
        <RobotView>
            <Typography color="resting" variant="h1">
                {TranslateText('Robot Status')}
            </Typography>
            <RobotCardSection>
                {robots.length > 0 && getRobotDisplay()}
                {robots.length === 0 && <RobotStatusCardPlaceholder />}
            </RobotCardSection>
        </RobotView>
    )
}
