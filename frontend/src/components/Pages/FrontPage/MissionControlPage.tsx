import { MissionQueueView } from 'components/Pages/FrontPage/MissionOverview/MissionQueueView'
import { OngoingMissionView } from 'components/Pages/FrontPage/MissionOverview/OngoingMissionView'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { StopRobotDialog } from './MissionOverview/StopDialogs'
import { StyledPage } from 'components/Styles/StyledComponents'

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

export const MissionControlPage = () => {
    return (
        <>
            <Header page={'missionControlPage'} />
            <StyledPage>
                <StopRobotDialog />
                <HorizontalContent>
                    <MissionsContent>
                        <OngoingMissionView />
                        <MissionQueueView />
                    </MissionsContent>
                </HorizontalContent>
            </StyledPage>
        </>
    )
}
