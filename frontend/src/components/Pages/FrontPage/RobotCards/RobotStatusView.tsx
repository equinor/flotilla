import { Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { useErrorHandler } from 'react-error-boundary'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotStatusCard'
import { Text } from 'components/Contexts/LanguageContext'

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
    const handleError = useErrorHandler()

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
        BackendAPICaller.getEnabledRobots().then((result: Robot[]) => {
            setRobots(sortRobotsByStatus(result))
        })
        //.catch((e) => handleError(e))
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
                {Text('Robot Status')}
            </Typography>
            <RobotCardSection>
                {robots.length > 0 && robotDisplay}
                {robots.length === 0 && <RobotStatusCardPlaceholder />}
            </RobotCardSection>
        </RobotView>
    )
}
