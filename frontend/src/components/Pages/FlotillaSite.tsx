import { config } from 'config'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage } from './FrontPage/FrontPage'
import { MissionPage } from './MissionPage/MissionPage'
import { InstallationProvider } from 'components/Contexts/InstallationContext'
import { MissionHistoryPage } from './MissionHistoryPage/MissionHistoryPage'
import { RobotPage } from './RobotPage/RobotPage'
import { AuthProvider } from 'components/Contexts/AuthProvider'
import { APIUpdater } from 'components/Contexts/APIUpdater'

export function FlotillaSite() {
    return (
        <>
            <InstallationProvider>
                <AuthProvider>
                    <APIUpdater>
                        <BrowserRouter>
                            <Routes>
                                <Route path={`${config.FRONTEND_BASE_ROUTE}/`} element={<FrontPage />} />
                                <Route
                                    path={`${config.FRONTEND_BASE_ROUTE}/mission/:missionId`}
                                    element={<MissionPage />}
                                />
                                <Route
                                    path={`${config.FRONTEND_BASE_ROUTE}/history`}
                                    element={<MissionHistoryPage />}
                                />
                                <Route path={`${config.FRONTEND_BASE_ROUTE}/robot/:robotId`} element={<RobotPage />} />
                            </Routes>
                        </BrowserRouter>
                    </APIUpdater>
                </AuthProvider>
            </InstallationProvider>
        </>
    )
}
