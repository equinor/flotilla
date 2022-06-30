import { Typography } from '@equinor/eds-core-react'
import { useApi, useInterval } from 'api/ApiCaller'
import { defaultRobots, Robot } from 'models/robot'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { RobotStatusCard, RobotStatusCardPlaceholder } from './RobotStatusCard'
const testRobots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

const refreshTimer = 5000

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
    const [robots, setRobots] = useState<Robot[]>([])
    useEffect(() => {
        setRobots(testRobots)
    }, [])
    console.log(robots)
    var robotDisplay = robots.map(function (robot, index) {
        return <RobotStatusCard key={index} robot={robot} />
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
