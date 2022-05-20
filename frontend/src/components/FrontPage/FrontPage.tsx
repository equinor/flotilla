import { Typography, Button } from '@equinor/eds-core-react'
import { RobotOverview } from 'components/RobotOverview'
import { ProfileContent } from 'components/SignInPage/ProfileContent'
import { defaultRobots } from 'models/robot'

const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

export function FrontPage() {
    return (
        <>
            <ProfileContent />
            <div className="test-button">
                <Button href="test">To Test Page</Button>
            </div>
            <div className="header">
                <Typography color="primary" variant="h1" bold>
                    Flotilla
                </Typography>
            </div>
            <div className="robot-overview">
                <RobotOverview robots={robots}></RobotOverview>
            </div>
            <div className="mission-overview">
                <Typography variant="h2" style={{ marginTop: '20px' }}>
                    Mission Overview
                </Typography>
            </div>
        </>
    )
}
