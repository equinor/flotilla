import { MissionQueueView } from 'components/Pages/FrontPage/MissionOverview/MissionQueueView'
import { OngoingMissionView } from 'components/Pages/FrontPage/MissionOverview/OngoingMissionView'
import { RobotStatusSection } from 'components/Pages/FrontPage/RobotCards/RobotStatusSection'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { StopRobotDialog } from './MissionOverview/StopDialogs'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100%, 1fr));
    gap: 3rem;
    margin: 8px;
`

const HorizontalContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    grid-template-columns: auto auto;
    gap: 2rem;
`

const MissionsContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    flex-direction: row;
    gap: 2rem;
`

export const FrontPage = () => {
    return (
        <>
            <Header page={'frontPage'} />
            <StyledFrontPage>
                <StopRobotDialog />
                <HorizontalContent>
                    <MissionsContent>
                        <OngoingMissionView />
                        <MissionQueueView />
                    </MissionsContent>
                </HorizontalContent>
                <RobotStatusSection />
                <InspectionOverviewSection />
            </StyledFrontPage>
        </>
    )
}
