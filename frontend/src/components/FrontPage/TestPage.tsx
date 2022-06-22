import { Button } from '@equinor/eds-core-react'
import { MissionView } from 'components/MissionOverview/MissionView'
import { RobotStatusSection } from 'components/RobotCards/RobotStatusSection'
import { useApi } from 'components/SignInPage/ApiCaller'
import { defaultRobots } from 'models/robot'
import styled from 'styled-components'
const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

const FrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1rem;
`

export function TestPage() {
    const apiCaller = useApi()
    var backendRobots
    return (
        <FrontPage>
            <div>
                <h1>This is a test page</h1>
            </div>
            <MissionView />
            <RobotStatusSection />
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
        </FrontPage>
    )
}
