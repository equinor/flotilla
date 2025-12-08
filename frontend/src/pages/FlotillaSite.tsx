import { config } from 'config'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage, TabNames } from './FrontPage/FrontPage'
import { APIUpdater } from 'components/Contexts/APIUpdater'
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
import { PageRouter } from './PageRouter'
import { PageNotFound } from './NotFoundPage'
import { useAssetContext } from 'components/Contexts/AssetContext'

export const FlotillaSite = () => {
    const frontPageTabOptions = Object.values(TabNames)
    const { installationCode } = useAssetContext()

    return (
        <>
            <APIUpdater>
                <BrowserRouter>
                    <Routes>
                        <Route path={`${config.FRONTEND_BASE_ROUTE}`} element={<AssetSelectionPage />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/`} element={<AssetSelectionPage />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/${installationCode}/`}>
                            <Route path={`front-page`} element={<FrontPage activeTab={frontPageTabOptions[0]} />} />
                            {frontPageTabOptions.map((tab) => (
                                <Route key={tab} path={`front-page-${tab}`} element={<FrontPage activeTab={tab} />} />
                            ))}
                            <Route path={`history`} element={<MissionHistoryPage />} />
                            <Route path={`mission-control`} element={<MissionControlPage />} />
                            <Route path={`inspection-overview`} element={<AreaOverviewPage />} />
                            <Route path={`predefined-missions`} element={<PredefinedMissionsPage />} />
                            <Route path={`auto-schedule`} element={<AutoSchedulePage />} />
                            <Route path={`robots`} element={<RobotStatusPage />} />
                        </Route>
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/:page`} element={<PageRouter />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/info`} element={<InfoPage />} />
                        <Route path="*" element={<PageNotFound />} />
                    </Routes>
                </BrowserRouter>
            </APIUpdater>
        </>
    )
}
