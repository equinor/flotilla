import { Button } from '@equinor/eds-core-react'
import { MissionView } from 'components/MissionOverview/MissionView'
import { OngoingMissionView } from 'components/MissionOverview/OngoingMissionView'
import { RobotStatusSection } from 'components/RobotCards/RobotStatusSection'
import { useApi } from 'api/ApiCaller'
import styled from 'styled-components'
import path from 'path'
import { Link } from 'react-router-dom'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1rem;
`

export function FrontPage() {
    const apiCaller = useApi()
    return (
        <StyledFrontPage>
            <OngoingMissionView />
            <MissionView />
            <RobotStatusSection />
            <div>
                <Button
                    variant="contained"
                    onClick={() => {
                        apiCaller.getRobots().then((robots) => {
                            console.log(robots)
                        })
                    }}
                >
                    Test Backend
                </Button>
                <Button
                    href="mission"
                >
                    Mission Page
                </Button>
            </div>
        </StyledFrontPage>
    )
}
