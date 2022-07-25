import { Button } from '@equinor/eds-core-react'
import { MissionView } from 'components/MissionOverview/MissionView'
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
            <MissionView />
            <RobotStatusSection />
            <div>
                <Button
                    variant="contained"
                    onClick={() => {
                        apiCaller.getReports(asset).then((reports) => {
                            console.log(reports)
                        })
                    }}
                >
                    Test Backend
                </Button>
                <Button href="mission">Mission Page</Button>
            </div>
        </StyledFrontPage>
    )
}
