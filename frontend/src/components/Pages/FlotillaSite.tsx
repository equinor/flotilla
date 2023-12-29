import { config } from 'config'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage } from './FrontPage/FrontPage'
import { MissionPage } from './MissionPage/MissionPage'
import { MissionHistoryPage } from './MissionHistoryPage/MissionHistoryPage'
import { RobotPage } from './RobotPage/RobotPage'
import { APIUpdater } from 'components/Contexts/APIUpdater'
import { MissionDefinitionPage } from './MissionDefinitionPage/MissionDefinitionPage'
import { AssetSelectionPage } from './AssetSelectionPage/AssetSelectionPage'

export const FlotillaSite = () => {
    return (
        <>
            <APIUpdater>
                <BrowserRouter>
                    <Routes>
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/`} element={<AssetSelectionPage />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/FrontPage`} element={<FrontPage />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/mission/:missionId`} element={<MissionPage />} />
                        <Route
                            path={`${config.FRONTEND_BASE_ROUTE}/mission-definition/:missionId`}
                            element={<MissionDefinitionPage />}
                        />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/history`} element={<MissionHistoryPage />} />
                        <Route path={`${config.FRONTEND_BASE_ROUTE}/robot/:robotId`} element={<RobotPage />} />
                    </Routes>
                </BrowserRouter>
            </APIUpdater>
        </>
    )
}
