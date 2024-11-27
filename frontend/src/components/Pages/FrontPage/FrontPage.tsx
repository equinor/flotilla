import { MissionQueueView } from 'components/Pages/FrontPage/MissionOverview/MissionQueueView'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { StopRobotDialog } from './MissionOverview/StopDialogs'
import { tokens } from '@equinor/eds-tokens'
import { MissionControlSection } from './MissionOverview/MissionControlSection'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100%, 1fr));
    gap: 3rem;
    padding: 15px 15px;
    background-color: ${tokens.colors.ui.background__light.hex};
`

const HorizontalContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    grid-template-columns: auto auto;
    gap: 2rem;
`

export const FrontPage = () => {
    return (
        <>
            <Header page={'frontPage'} />
            <StyledFrontPage>
                <StopRobotDialog />
                <HorizontalContent>
                    <MissionControlSection />
                    <MissionQueueView />
                </HorizontalContent>
                <InspectionOverviewSection />
            </StyledFrontPage>
        </>
    )
}
