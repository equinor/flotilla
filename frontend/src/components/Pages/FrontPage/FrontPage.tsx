import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { StopRobotDialog } from './MissionOverview/StopDialogs'
import { tokens } from '@equinor/eds-tokens'
import { MissionControlSection } from './MissionOverview/MissionControlSection'
import { redirectIfNoInstallationSelected } from 'utils/RedirectIfNoInstallationSelected'
import { AutoScheduleSection } from './AutoScheduleSection/AutoScheduleSection'
import { useState } from 'react'
import { Icon, Tabs, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionSection } from '../InspectionPage/InspectionSection'
import { MissionHistoryView } from '../MissionHistory/MissionHistoryView'
import { RobotStatusSection } from '../RobotCards/RobotStatusSection'
import { Icons } from 'utils/icons'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { useInstallationContext } from 'components/Contexts/InstallationContext'

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
const StyledTabHeader = styled.div`
    display: flex;
    justify-content: space-between;
`
const StyledTabHeaderRightContent = styled.div`
    display: flex;
    align-items: center;
    gap: 24px;
    align-self: stretch;
`
const StyledOngoingMissionsInfo = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    cursor: pointer;
`
const StyledNumberOfMissions = styled.div`
    display: flex;
    text-wrap: nowrap;
    flex-direction: row;
`
const StyledTabsList = styled(Tabs.List)`
    display: flex;
    flex-wrap: wrap;
`

const OngoingMissionsInfo = ({ goToOngoingTab }: { goToOngoingTab: () => void }) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions } = useMissionsContext()

    const areaNames = new Set(
        ongoingMissions.map((m) => m.inspectionArea.inspectionAreaName).filter((area) => area !== undefined)
    )
    const formattedAreaNames = Array.from(areaNames).join(' | ')

    return (
        <StyledOngoingMissionsInfo onClick={goToOngoingTab}>
            <StyledNumberOfMissions>
                <Icon name={Icons.Ongoing} size={24} />
                <Typography variant="h5">
                    {' '}
                    {`${ongoingMissions.length} ${TranslateText('Ongoing missions')}`}
                </Typography>
            </StyledNumberOfMissions>
            <Typography variant="body_short"> {formattedAreaNames} </Typography>
        </StyledOngoingMissionsInfo>
    )
}

export enum TabNames {
    MissionControl = 'mission-control',
    InspectionPlan = 'inspection-plan',
    PredefinedMissions = 'predefined-missions',
    MissionHistory = 'mission-history',
    AutoScheduling = 'auto-scheduling',
    Robots = 'robots',
}

export const FrontPage = ({ initialTab }: { initialTab: TabNames }) => {
    const [activeTab, setActiveTab] = useState(initialTab)
    const { TranslateText } = useLanguageContext()
    const { installationInspectionAreas } = useInstallationContext()

    redirectIfNoInstallationSelected()

    const navigate = useNavigate()
    const goToTab = (tabIndex: number) => {
        const tabName = Object.values(TabNames)[tabIndex]
        setActiveTab(tabName)
        const path = `${config.FRONTEND_BASE_ROUTE}/front-page-${tabName}`
        navigate(path)
    }
    const getIndexFromTabName = (tabName: TabNames) => {
        return Object.values(TabNames).indexOf(tabName)
    }

    const setActiveTabToMissionControl = () => goToTab(getIndexFromTabName(TabNames.MissionControl))

    return (
        <>
            <Header page={'frontPage'} />
            <StyledFrontPage>
                <Tabs activeTab={getIndexFromTabName(activeTab)} onChange={goToTab}>
                    <StyledTabHeader>
                        <StyledTabsList>
                            <Tabs.Tab>{TranslateText('Mission Control')}</Tabs.Tab>
                            {installationInspectionAreas.length > 1 ? (
                                <Tabs.Tab>{TranslateText('Area Overview')}</Tabs.Tab>
                            ) : (
                                <></>
                            )}
                            <Tabs.Tab>{TranslateText('Predefined Missions')}</Tabs.Tab>
                            <Tabs.Tab>{TranslateText('Mission History')}</Tabs.Tab>
                            <Tabs.Tab>{TranslateText('Auto Scheduling')}</Tabs.Tab>
                            <Tabs.Tab>{TranslateText('Robots')}</Tabs.Tab>
                        </StyledTabsList>
                        <StyledTabHeaderRightContent>
                            <OngoingMissionsInfo goToOngoingTab={setActiveTabToMissionControl} />
                            <StopRobotDialog />
                        </StyledTabHeaderRightContent>
                    </StyledTabHeader>
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
