import { UpcomingMissionView } from 'components/Pages/FrontPage/MissionOverview/UpcomingMissionView'
import { OngoingMissionView } from 'components/Pages/FrontPage/MissionOverview/OngoingMissionView'
import { RobotStatusSection } from 'components/Pages/FrontPage/RobotCards/RobotStatusView'
import { FailedMissionAlertView } from './MissionOverview/FailedMissionAlertView'
import styled from 'styled-components'
import { PastMissionView } from './MissionOverview/PastMissionsView'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100%, 1fr));
    gap: 3rem;
`

const HorizontalContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    grid-template-columns: 900px auto;
    gap: 2rem;
`

const MissionsContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    flex-direction: column;
    gap: 2rem;
`

export type RefreshProps = {
    refreshInterval: number
}

export function FrontPage() {
    const refreshInterval = 1000

    return (
        <StyledFrontPage>
            <FailedMissionAlertView refreshInterval={refreshInterval} />
            <HorizontalContent>
                <MissionsContent>
                    <OngoingMissionView refreshInterval={refreshInterval} />
                    <UpcomingMissionView refreshInterval={refreshInterval} />
                </MissionsContent>
                <PastMissionView refreshInterval={refreshInterval} />
            </HorizontalContent>
            <RobotStatusSection refreshInterval={refreshInterval} />
        </StyledFrontPage>
    )
}
