import { Typography } from '@equinor/eds-core-react'
import { useApi } from 'components/SignInPage/ApiCaller'
import { defaultRobots, Robot } from 'models/robot'
import styled from 'styled-components'
import { RobotStatusCard } from './RobotStatusCard'
const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

const RobotCards = styled.div`
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
    // var backendRobots = apiCaller.getRobots()
    var defaultRobots = robots.map(function (robot, index) {
        return <RobotStatusCard key={index} robot={robot} />
    })
    return (
        <RobotView>
            <Typography color="resting" variant="h2">
                Robot status
            </Typography>
            <RobotCards>{defaultRobots}</RobotCards>
        </RobotView>
    )
}
