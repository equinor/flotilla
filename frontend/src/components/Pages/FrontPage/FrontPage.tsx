import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { StopRobotDialog } from './MissionOverview/StopDialogs'
import { tokens } from '@equinor/eds-tokens'
import { MissionControlSection } from './MissionOverview/MissionControlSection'
import { useInstallationContext } from 'components/Contexts/InstallationContext'

const StyledFrontPage = styled.div`
    display: flex;
    flex-direction: column;
    gap: 3rem;
    padding: 15px 15px;
    background-color: ${tokens.colors.ui.background__light.hex};
    min-height: calc(100vh - 65px);
`

export const FrontPage = () => {
    const { installationCode } = useInstallationContext()

    return (
        <>
            <Header page={'frontPage'} />
            {installationCode !== '' && (
                <StyledFrontPage>
                    <StopRobotDialog />
                    <MissionControlSection />
                    <InspectionOverviewSection />
                </StyledFrontPage>
            )}
        </>
    )
}
