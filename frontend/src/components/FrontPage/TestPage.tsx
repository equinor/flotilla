import { Button } from '@equinor/eds-core-react'
import { RobotStatusCard } from 'components/RobotOverview/RobotStatusCard'
import { useApi } from 'components/SignInPage/ApiCaller'
import { defaultRobots } from 'models/robot'
const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

export function TestPage() {
    const apiCaller = useApi()
    var backendRobots
    var defaultRobots = robots.map(function (robot) {
        return <RobotStatusCard robot={robot} />
    })
    return (
        <>
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

            {/* <RobotStatusCard robot={robots[0]} /> */}
        </>
    )
}
