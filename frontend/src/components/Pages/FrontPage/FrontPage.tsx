import { UpcomingMissionView } from 'components/Pages/FrontPage/MissionOverview/UpcomingMissionView'
import { OngoingMissionView } from 'components/Pages/FrontPage/MissionOverview/OngoingMissionView'
import { RobotStatusSection } from 'components/Pages/FrontPage/RobotCards/RobotStatusView'
import { FailedMissionAlertView } from './MissionOverview/FailedMissionAlertView'
import styled from 'styled-components'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100%, 1fr));
    gap: 3rem;
`

export type RefreshProps = {
    refreshInterval: number
}

export function FrontPage() {
    const refreshInterval = 1000

    return (
        <StyledFrontPage>
            <FailedMissionAlertView refreshInterval={refreshInterval} />
            <OngoingMissionView refreshInterval={refreshInterval} />
            <UpcomingMissionView refreshInterval={refreshInterval} />
            <RobotStatusSection refreshInterval={refreshInterval} />
        </StyledFrontPage>
    )
}
