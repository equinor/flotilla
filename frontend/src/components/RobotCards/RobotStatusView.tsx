import { Typography } from '@equinor/eds-core-react'
import { useApi, useInterval } from 'api/ApiCaller'
import { Robot } from 'models/robot'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
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

export function RobotStatusSection() {
    const apiCaller = useApi()

    const [robots, setRobots] = useState<Robot[]>([])
    useEffect(() => {
        apiCaller.getRobots().then((result) => {
            setRobots(result)
        })
    }, [])

    useInterval(async () => {
        apiCaller.getRobots().then((result) => {
            setRobots(result)
        })
    })
    var robotDisplay = robots.map(function (robot) {
        return <RobotStatusCard key={robot.id} robot={robot} />
    })
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
