import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { StopRobotDialog } from './MissionOverview/StopDialogs'
import { tokens } from '@equinor/eds-tokens'
import { MissionControlSection } from './MissionOverview/MissionControlSection'
import { redirectIfNoInstallationSelected } from 'utils/RedirectIfNoInstallationSelected'
import { AutoScheduleSection } from './AutoScheduleSection/AutoScheduleSection'
import { useState, useMemo, type ReactNode } from 'react'
import { Icon, Tabs, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionSection } from '../InspectionPage/InspectionSection'
import { MissionHistoryView } from '../MissionHistory/MissionHistoryView'
import { RobotStatusSection } from '../RobotCards/RobotStatusSection'
import { Icons } from 'utils/icons'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { phone_width } from 'utils/constants'
import { MissionStats } from '../InstallationStats/InstallationstatsView'
import { useAssetContext } from 'components/Contexts/RobotContext'

const StyledFrontPage = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 20px 20px;
    background-color: ${tokens.colors.ui.background__light.hex};
    min-height: calc(100vh - 65px);

    @media (max-width: ${phone_width}) {
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
                <Typography variant="h5">{`${ongoingMissions.length} ${TranslateText('Ongoing missions')}`}</Typography>
            </StyledNumberOfMissions>
            <Typography variant="body_short">{formattedAreaNames}</Typography>
        </StyledOngoingMissionsInfo>
    )
}

export enum TabNames {
    MissionControl = 'mission-control',
    InspectionPlan = 'inspection-overview',
    PredefinedMissions = 'predefined-missions',
    MissionHistory = 'mission-history',
    AutoScheduling = 'auto-scheduling',
    Robots = 'robots',
    Statistics = 'statistics',
}

type TabDef = {
    name: TabNames
    label: string
    render: () => ReactNode
}

export const FrontPage = ({ initialTab }: { initialTab: TabNames }) => {
    const [activeTab, setActiveTab] = useState<TabNames>(initialTab)
    const { TranslateText } = useLanguageContext()
    const { installationInspectionAreas } = useAssetContext()

    redirectIfNoInstallationSelected()

    const navigate = useNavigate()

    const tabs: TabDef[] = useMemo(() => {
        const list: TabDef[] = [
            {
                name: TabNames.MissionControl,
                label: TranslateText('Mission Control'),
                render: () => <MissionControlSection />,
            },
            ...(installationInspectionAreas.length > 1
                ? [
                      {
                          name: TabNames.InspectionPlan,
                          label: TranslateText('Area Overview'),
                          render: () => <InspectionSection />,
                      },
                  ]
                : []),
            {
                name: TabNames.PredefinedMissions,
                label: TranslateText('Predefined Missions'),
                render: () => <InspectionOverviewSection />,
            },
            {
                name: TabNames.MissionHistory,
                label: TranslateText('Mission History'),
                render: () => <MissionHistoryView refreshInterval={1000} />,
            },
            {
                name: TabNames.AutoScheduling,
                label: TranslateText('Auto Scheduling'),
                render: () => <AutoScheduleSection />,
            },
            { name: TabNames.Robots, label: TranslateText('Robots'), render: () => <RobotStatusSection /> },
            { name: TabNames.Statistics, label: TranslateText('Statistics'), render: () => <MissionStats /> },
        ]
        return list
    }, [installationInspectionAreas.length, TranslateText])

    const activeIndex = tabs.findIndex((t) => t.name === activeTab)

    const goToTab = (index: number | string) => {
        let tab
        if (typeof index === 'number') tab = tabs[index]
        else tab = tabs.find((t) => t.name === index)

        if (tab === undefined) return

        setActiveTab(tab.name)
        navigate(`${config.FRONTEND_BASE_ROUTE}/front-page-${tab.name}`)
    }

    const setActiveTabToMissionControl = () => goToTab(tabs.findIndex((t) => t.name === TabNames.MissionControl))

    return (
        <>
            <Header page={'frontPage'} />
            <StyledFrontPage>
                <Tabs activeTab={activeIndex} onChange={goToTab}>
                    <StyledTabHeader>
                        <StyledTabsList>
                            {tabs.map((t) => (
                                <Tabs.Tab key={t.name}>{t.label}</Tabs.Tab>
                            ))}
                        </StyledTabsList>
                        <StyledTabHeaderRightContent>
                            <OngoingMissionsInfo goToOngoingTab={setActiveTabToMissionControl} />
                            <StopRobotDialog />
                        </StyledTabHeaderRightContent>
                    </StyledTabHeader>

                    {tabs[activeIndex]?.render()}
                </Tabs>
            </StyledFrontPage>
        </>
    )
}
