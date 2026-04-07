import { config } from 'config'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AssetSelectionPage } from './AssetSelectionPage/AssetSelectionPage'
import { InfoPage } from './InfoPage'
import { MissionDefinitionPageRouter, MissionPageRouter, RobotPageRouter, SimpleMissionPageRouter } from './PageRouter'
import { PageNotFound } from './NotFoundPage'
import { DataViewPage } from './MissionHistory/DataViewPage'
import { InstallationLayout } from 'components/Contexts/InstallationContext'
import { MissionControlPage } from './MissionControlPage'
import { AreaOverviewPage } from './AreaOverviewPage'
import { PredefinedMissionsPage } from './PredefinedMissionsPage'
import { MissionHistoryPage } from './MissionHistoryPage'
import { AutoSchedulePage } from './AutoSchedulePage'
import { RobotStatusPage } from './RobotStatusPage'

export const FlotillaSite = () => {
    return (
        <>
            <BrowserRouter basename={config.FRONTEND_BASE_ROUTE}>
                <Routes>
                    <Route path="/" element={<AssetSelectionPage />} />
                    <Route path=":installationCode" element={<InstallationLayout />}>
                        <Route index element={<Navigate to="mission-control" replace />} />
                        <Route path="mission-control" element={<MissionControlPage />} />
                        <Route path="inspection-overview" element={<AreaOverviewPage />} />
                        <Route path="predefined-missions" element={<PredefinedMissionsPage />} />
                        <Route path="history" element={<MissionHistoryPage />} />
                        <Route path="auto-schedule" element={<AutoSchedulePage />} />
                        <Route path="robots" element={<RobotStatusPage />} />
                        <Route path="data-view" element={<DataViewPage />} />
                        <Route path="mission/:missionId" element={<MissionPageRouter />} />
                        <Route path="mission-simple/:missionId" element={<SimpleMissionPageRouter />} />
                        <Route path="missiondefinition/:missionId" element={<MissionDefinitionPageRouter />} />
                        <Route path="robot/:robotId" element={<RobotPageRouter />} />
                    </Route>
                    <Route path="/info" element={<InfoPage />} />
                    <Route path="/not-found" element={<PageNotFound />} />
                    <Route path="*" element={<PageNotFound />} />
                </Routes>
            </BrowserRouter>
        </>
    )
}
