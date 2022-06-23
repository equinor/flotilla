import { Button } from '@equinor/eds-core-react'
import { MissionView } from 'components/MissionOverview/MissionView'
import { RobotStatusSection } from 'components/RobotCards/RobotStatusSection'
import { useApi } from 'components/SignInPage/ApiCaller'
import styled from 'styled-components'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1rem;
`

export function FrontPage() {
    const apiCaller = useApi()
    return (
        <StyledFrontPage>
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
            </div>
        </StyledFrontPage>
    )
}
