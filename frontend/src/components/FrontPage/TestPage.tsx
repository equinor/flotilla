import { Button } from '@equinor/eds-core-react'
import { MissionQueueTable } from 'components/MissionOverview/MissionQueue'
import { RobotStatusCard } from 'components/RobotCards/RobotStatusCard'
import { useApi } from 'components/SignInPage/ApiCaller'
import { defaultRobots } from 'models/robot'
import styled from 'styled-components'
const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

const FrontPage = styled.div`
    display: grid;
`

export function TestPage() {
    const apiCaller = useApi()
    var backendRobots
    var defaultRobots = robots.map(function (robot) {
        return <RobotStatusCard robot={robot} />
    })
    return (
        <FrontPage>
            <div>
                <h1>This is a test page</h1>
            </div>
            {defaultRobots}
            <div>
                <Button
                    variant="contained"
                    onClick={() => {
                        backendRobots = apiCaller.getRobots()
                    }}
                >
                    Test Backend
                </Button>
            </div>
            <MissionQueueTable />

            {/* <RobotStatusCard robot={robots[0]} /> */}
        </FrontPage>
    )
}
