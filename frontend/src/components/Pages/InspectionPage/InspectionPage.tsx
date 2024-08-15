import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { tokens } from '@equinor/eds-tokens'

const StyledFrontPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100%, 1fr));
    gap: 3rem;
    padding: 15px 15px;
    background-color: ${tokens.colors.ui.background__light.hex};
    min-height: calc(100vh - 65px);
`

export const InspectionPage = () => {
    return (
        <>
            <Header page={'inspectionPage'} />
            <StyledFrontPage>
                <InspectionOverviewSection />
            </StyledFrontPage>
        </>
    )
}
