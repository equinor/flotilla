import { Button } from '@equinor/eds-core-react'
import { UpcomingMissionView } from 'components/MissionOverview/UpcomingMissionView'
import { OngoingMissionView } from 'components/MissionOverview/OngoingMissionView'
import { RobotStatusSection } from 'components/RobotCards/RobotStatusView'
import { useApi } from 'api/ApiCaller'
import styled from 'styled-components'
import { useAssetContext } from 'components/Contexts/AssetContext'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 3rem;
`

export function FrontPage() {
    const apiCaller = useApi()
    const { asset, switchAsset } = useAssetContext()
    return (
        <StyledFrontPage>
            <OngoingMissionView />
            <UpcomingMissionView />
            <RobotStatusSection />
            <div>
                <Button
                    variant="contained"
                    onClick={() => {
                        apiCaller.getAllEchoMissions().then((robots) => {
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
