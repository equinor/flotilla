import { Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useCallback, useEffect, useState } from 'react'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotStatusCard'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useSafeZoneContext } from 'components/Contexts/SafeZoneContext'

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
export function RobotStatusSection({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [robots, setRobots] = useState<Robot[]>([])
    const { safeZoneStatus, switchSafeZoneStatus } = useSafeZoneContext()

    const sortRobotsByStatus = useCallback((robots: Robot[]): Robot[] => {
        const sortedRobots = robots.sort((robot, robotToCompareWith) =>
            robot.status! > robotToCompareWith.status! ? 1 : -1
        )
        return sortedRobots
    }, [])

    const updateRobots = useCallback(() => {
        BackendAPICaller.getEnabledRobots().then((result: Robot[]) => {
            setRobots(sortRobotsByStatus(result))
            const missionQueueFozenStatus = result
                .map((robot: Robot) => {
                    return robot.missionQueueFrozen
                })
                .filter((status) => status === true)

            if (missionQueueFozenStatus.length > 0 && !safeZoneStatus) switchSafeZoneStatus(true)
            else switchSafeZoneStatus(false)
        })
    }, [sortRobotsByStatus])

    useEffect(() => {
        updateRobots()
    }, [updateRobots])

    useEffect(() => {
        const id = setInterval(() => {
            updateRobots()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval, updateRobots])

    const filteredRobots = robots.filter(function (robot) {
        return (
            robot.currentInstallation.toLocaleLowerCase() === installationCode.toLocaleLowerCase() ||
            (typeof robot.currentInstallation === 'string' && robot.currentInstallation.includes('default')) ||
            robot.currentInstallation === undefined
        )
    })

    const getRobotDisplay = () => {
        if (installationCode === '') {
            return robots.map(function (robot) {
                return <RobotStatusCard key={robot.id} robot={robot} />
            })
        } else {
            return filteredRobots.map(function (robot) {
                return <RobotStatusCard key={robot.id} robot={robot} />
            })
        }
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
