import { Typography } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotStatusCard'

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
    const apiCaller = useApi()

    const [robots, setRobots] = useState<Robot[]>([])
    useEffect(() => {
        updateRobots()
    }, [])

    useEffect(() => {
        const id = setInterval(() => {
            updateRobots()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    const updateRobots = () => {
        apiCaller.getRobots().then((result: Robot[]) => {
            setRobots(sortRobotsByStatus(result))
        })
    }

    var robotDisplay = robots.map(function (robot) {
        return <RobotStatusCard key={robot.id} robot={robot} />
    })
    const sortRobotsByStatus = (robots: Robot[]): Robot[] => {
        const sortedRobots = robots.sort((robot, robotToCompareWith) =>
            robot.status! > robotToCompareWith.status! ? 1 : -1
        )

        return sortedRobots
    }
    return (
        <RobotView>
            <Typography color="resting" variant="h2">
                Robot status
            </Typography>
            <RobotCardSection>
                {robots.length > 0 && robotDisplay}
                {robots.length === 0 && <RobotStatusCardPlaceholder />}
            </RobotCardSection>
        </RobotView>
    )
}
