import { config } from 'config'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage, TabNames } from './FrontPage/FrontPage'
import { AssetSelectionPage } from './AssetSelectionPage/AssetSelectionPage'
import {
    AutoSchedulePage,
    AreaOverviewPage,
    MissionControlPage,
    MissionHistoryPage,
    PredefinedMissionsPage,
    RobotStatusPage,
} from '../components/NavigationMenu/NavigationMenuPages'
import { InfoPage } from './InfoPage'
import { MissionDefinitionPageRouter, MissionPageRouter, RobotPageRouter, SimpleMissionPageRouter } from './PageRouter'
import { PageNotFound } from './NotFoundPage'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { DataViewPage } from './MissionHistory/DataViewPage'

export const FlotillaSite = () => {
    const frontPageTabOptions = Object.values(TabNames)
    const { installationCode } = useAssetContext()

    const installationCodePath = `${config.FRONTEND_BASE_ROUTE}/${installationCode}`

    // This needs to be a normal object so that it isn't interpreted as a non "Route" object
    const installationSpecificPages = (
        <>
            <Route
                path={`${installationCodePath}:front-page`}
                element={<FrontPage activeTab={frontPageTabOptions[0]} />}
            />
            {frontPageTabOptions.map((tab) => (
                <Route
                    key={tab}
                    path={`${installationCodePath}:front-page-${tab}`}
                    element={<FrontPage activeTab={tab} />}
                />
            ))}
            <Route path={`${installationCodePath}:history`} element={<MissionHistoryPage />} />
            <Route path={`${installationCodePath}:mission-control`} element={<MissionControlPage />} />
            <Route path={`${installationCodePath}:inspection-overview`} element={<AreaOverviewPage />} />
            <Route path={`${installationCodePath}:predefined-missions`} element={<PredefinedMissionsPage />} />
            <Route path={`${installationCodePath}:auto-schedule`} element={<AutoSchedulePage />} />
            <Route path={`${installationCodePath}:robots`} element={<RobotStatusPage />} />
            <Route path={`${installationCodePath}:mission`} element={<MissionPageRouter />} />
            <Route path={`${installationCodePath}:mission-simple`} element={<SimpleMissionPageRouter />} />
            <Route path={`${installationCodePath}:missiondefinition`} element={<MissionDefinitionPageRouter />} />
            <Route path={`${installationCodePath}:robot`} element={<RobotPageRouter />} />
            <Route path={`${installationCodePath}:data-view`} element={<DataViewPage />} />
        </>
    )

    return (
        <>
            <BrowserRouter>
                <Routes>
                    <Route path={`${config.FRONTEND_BASE_ROUTE}/`} element={<AssetSelectionPage />} />

                    {installationCode ? installationSpecificPages : <></>}

                    <Route path={`${config.FRONTEND_BASE_ROUTE}/info`} element={<InfoPage />} />
                    <Route path="*" element={<PageNotFound />} />
                </Routes>
            </BrowserRouter>
        </>
    )
}
