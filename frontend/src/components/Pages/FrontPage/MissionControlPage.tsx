import { MissionQueueView } from 'components/Pages/FrontPage/MissionOverview/MissionQueueView'
import { OngoingMissionView } from 'components/Pages/FrontPage/MissionOverview/OngoingMissionView'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { StopRobotDialog } from './MissionOverview/StopDialogs'
import { tokens } from '@equinor/eds-tokens'

const StyledFrontPage = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 15px 15px;
    background-color: ${tokens.colors.ui.background__light.hex};
    min-height: calc(100vh - 65px);
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

export const MissionControlPage = () => {
    return (
        <>
            <Header page={'missionControlPage'} />
            <StyledFrontPage>
                <StopRobotDialog />
                <HorizontalContent>
                    <MissionsContent>
                        <OngoingMissionView />
                        <MissionQueueView />
                    </MissionsContent>
                </HorizontalContent>
            </StyledFrontPage>
        </>
    )
}
