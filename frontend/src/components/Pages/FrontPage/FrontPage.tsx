import { MissionQueueView } from 'components/Pages/FrontPage/MissionOverview/MissionQueueView'
import { OngoingMissionView } from 'components/Pages/FrontPage/MissionOverview/OngoingMissionView'
import { RobotStatusSection } from 'components/Pages/FrontPage/RobotCards/RobotStatusView'
import { FailedMissionAlertView } from './MissionOverview/FailedMissionAlertView'
import { Header } from 'components/Header/Header'
import { AssetDecksButton } from 'components/Pages/AssetDecks/AssetDecksButton'
import styled from 'styled-components'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100%, 1fr));
    gap: 3rem;
    margin: 2rem;
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
    flex-direction: row;
    gap: 2rem;
`

export type RefreshProps = {
    refreshInterval: number
}

export function FrontPage() {
    const refreshInterval = 1000

    return (
        <>
            <Header page={'root'} />
            <StyledFrontPage>
                <FailedMissionAlertView refreshInterval={refreshInterval} />
                <HorizontalContent>
                    <MissionsContent>
                        <OngoingMissionView refreshInterval={refreshInterval} />
                        <MissionQueueView refreshInterval={refreshInterval} />
                    </MissionsContent>
                </HorizontalContent>
                <RobotStatusSection refreshInterval={refreshInterval} />
                <AssetDecksButton refreshInterval={refreshInterval} />
            </StyledFrontPage>
        </>
    )
}
