import { Header } from 'components/Header/Header'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { StyledPage } from 'components/Styles/StyledComponents'

export const InspectionPage = () => {
    return (
        <>
            <Header page={'inspectionPage'} />
            <StyledPage>
                <InspectionOverviewSection />
            </StyledPage>
        </>
    )
}
