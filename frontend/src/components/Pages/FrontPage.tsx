import { UpcomingMissionView } from 'components/MissionOverview/UpcomingMissionView'
import { OngoingMissionView } from 'components/MissionOverview/OngoingMissionView'
import { RobotStatusSection } from 'components/RobotCards/RobotStatusView'
import styled from 'styled-components'
import { useAssetContext } from 'components/Contexts/AssetContext'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 3rem;
`

export function FrontPage() {
    const { asset, switchAsset } = useAssetContext()
    return (
        <StyledFrontPage>
            <OngoingMissionView />
            <UpcomingMissionView />
            <RobotStatusSection />
        </StyledFrontPage>
    )
}
