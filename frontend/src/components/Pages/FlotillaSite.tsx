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
} from '../NavigationMenu/NavigationMenuPages'
import { InfoPage } from './InfoPage'
import { PageRouter } from './PageRouter'

export const FlotillaSite = () => {
    const frontPageTabOptions = Object.values(TabNames)

    return (
        <>
            <APIUpdater>
                <BrowserRouter>
                    <Routes>
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/`} element={<AssetSelectionPage />} />
                        <Route
                            path={`${config.FRONTEND_BASE_ROUTE}/front-page`}
                            element={<FrontPage initialTab={frontPageTabOptions[0]} />}
                        />
                        {frontPageTabOptions.map((tab) => (
                            <Route
                                key={tab}
                                path={`${config.FRONTEND_BASE_ROUTE}/front-page-${tab}`}
                                element={<FrontPage initialTab={tab} />}
                            />
                        ))}
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/:page`} element={<PageRouter />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/history`} element={<MissionHistoryPage />} />
                        <Route
                            path={`${config.FRONTEND_BASE_ROUTE}/mission-control`}
                            element={<MissionControlPage />}
                        />
                        <Route
                            path={`${config.FRONTEND_BASE_ROUTE}/inspection-overview`}
                            element={<AreaOverviewPage />}
                        />
                        <Route
                            path={`${config.FRONTEND_BASE_ROUTE}/predefined-missions`}
                            element={<PredefinedMissionsPage />}
                        />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/auto-schedule`} element={<AutoSchedulePage />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/robots`} element={<RobotStatusPage />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/info`} element={<InfoPage />} />
                    </Routes>
                </BrowserRouter>
            </APIUpdater>
        </>
    )
}
