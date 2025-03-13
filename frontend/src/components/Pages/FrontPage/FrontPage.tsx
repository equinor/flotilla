import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { StopRobotDialog } from './MissionOverview/StopDialogs'
import { tokens } from '@equinor/eds-tokens'
import { MissionControlSection } from './MissionOverview/MissionControlSection'
import { redirectIfNoInstallationSelected } from 'utils/RedirectIfNoInstallationSelected'
import { AutoScheduleSection } from './AutoScheduleSection/AutoScheduleSection'
import { useState } from 'react'
import { Tabs } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionSection } from '../InspectionPage/InspectionSection'
import { MissionHistoryView } from '../MissionHistory/MissionHistoryView'
import { RobotStatusSection } from '../RobotCards/RobotStatusSection'

const StyledFrontPage = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 20px 20px;
    background-color: ${tokens.colors.ui.background__light.hex};
    min-height: calc(100vh - 65px);

    @media (max-width: 600px) {
        display: none;
    }
`

export const FrontPage = () => {
    const [activeTab, setActiveTab] = useState(0)
    const { TranslateText } = useLanguageContext()

    redirectIfNoInstallationSelected()

    return (
        <>
            <Header page={'frontPage'} />
            <StyledFrontPage>
                <StopRobotDialog />
                <Tabs activeTab={activeTab} onChange={setActiveTab}>
                    <Tabs.List>
                        <Tabs.Tab>{TranslateText('Mission Control')}</Tabs.Tab>
                        <Tabs.Tab>{TranslateText('Deck Overview')}</Tabs.Tab>
                        <Tabs.Tab>{TranslateText('Predefined Missions')}</Tabs.Tab>
                        <Tabs.Tab>{TranslateText('Mission History')}</Tabs.Tab>
                        <Tabs.Tab>{TranslateText('Auto Scheduling')}</Tabs.Tab>
                        <Tabs.Tab>{TranslateText('Robots')}</Tabs.Tab>
                    </Tabs.List>
                    <Tabs.Panels>
                        <Tabs.Panel>
                            <MissionControlSection />
                        </Tabs.Panel>
                        <Tabs.Panel>
                            <InspectionSection />
                        </Tabs.Panel>
                        <Tabs.Panel>
                            <InspectionOverviewSection />
                        </Tabs.Panel>
                        <Tabs.Panel>
                            <MissionHistoryView refreshInterval={1000} />
                        </Tabs.Panel>
                        <Tabs.Panel>
                            <AutoScheduleSection />
                        </Tabs.Panel>
                        <Tabs.Panel>
                            <RobotStatusSection />
                        </Tabs.Panel>
                    </Tabs.Panels>
                </Tabs>
            </StyledFrontPage>
        </>
    )
}
