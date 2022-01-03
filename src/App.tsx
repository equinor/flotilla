import { Icon, Typography } from '@equinor/eds-core-react'
import { platform } from '@equinor/eds-icons'
import { RobotOverview } from './components/RobotOverview'
import { defaultRobots } from './models/robot'
import './app.css'
import { defaultMissions } from 'models/mission'
import { MissionOverview } from 'components/MissionOverview'
Icon.add({ platform })

const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

const missions = [defaultMissions['turtlebot'], defaultMissions['testplan']]

function App() {
    return (
        <div className="app-ui">
            <div className="header">
                <Typography color="primary" variant="h1" bold>
                    Flotilla
                </Typography>
            </div>
            <div className="robot-overview">
                <RobotOverview robots={robots}></RobotOverview>
            </div>
            <div className="mission-overview">
                <MissionOverview missions={missions} robots={robots}></MissionOverview>
            </div>
        </div>
    )
}

export default App
