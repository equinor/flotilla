import { config } from 'config'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AssetSelectionPage } from './AssetSelectionPage'
import { InfoPage } from './InfoPage'
import { MissionDefinitionPageRouter, MissionPageRouter, RobotPageRouter, SimpleMissionPageRouter } from './PageRouter'
import { PageNotFound } from './NotFoundPage'
import { DataViewPage } from './MissionHistory/DataViewPage'
import { MissionControlPage } from './MissionControlPage'
import { AreaOverviewPage } from './AreaOverviewPage'
import { PredefinedMissionsPage } from './PredefinedMissionsPage'
import { MissionHistoryPage } from './MissionHistoryPage'
import { AutoSchedulePage } from './AutoSchedulePage'
import { RobotStatusPage } from './RobotStatusPage'
import { Outlet, useParams } from 'react-router-dom'
import { CircularProgress } from '@equinor/eds-core-react'
import { Typography } from '@equinor/eds-core-react'
import { InstallationContext, useInstallationOrUndefined } from 'components/Contexts/InstallationContext'
import { AssetProvider } from 'components/Contexts/AssetContext'
import { AlertProvider } from 'components/Contexts/AlertContext'
import { SignalRProvider } from 'components/Contexts/SignalRContext'
import { InspectionsProvider } from 'components/Contexts/InspectionsContext'
import { MissionDefinitionsProvider } from 'components/Contexts/MissionDefinitionsContext'
import { MissionRunsProvider } from 'components/Contexts/MissionRunsContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'
import { StatisticsPage } from './StatisticsPage'

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
                        <Route path="statistics" element={<StatisticsPage />} />
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

const InstallationLayout = () => {
    const { installationCode } = useParams()
    const installation = useInstallationOrUndefined(installationCode!)

    if (!installation) {
        return (
            <>
                <CircularProgress />
                <Typography variant="h2">Loading installation data...</Typography>
            </>
        )
    }

    return (
        <>
            <InstallationContext.Provider
                value={{
                    installation,
                }}
            >
                <SignalRProvider>
                    {/* SignalRProvider Needs to be within an installation in order to reset connections when changing installation */}
                    <AssetProvider>
                        {/* AssetProvider Requires knowing the installation and SignalR */}
                        <AlertProvider>
                            {/* AlertProvider Requires knowing the installation, signalR and assetContext */}
                            <InspectionsProvider>
                                {/* InspectionsProvider Requires SignalR, */}
                                <MissionDefinitionsProvider>
                                    {/* MissionDefinitionsProvider Requires SignalR, Alert and Installation */}
                                    <MissionRunsProvider>
                                        {/* MissionRunsProvider Requires SignalR, Alert and Installation */}
                                        <MissionControlProvider>
                                            {/* MissionControlProvider Requires Alert and AssetContext */}
                                            <Outlet />
                                        </MissionControlProvider>
                                    </MissionRunsProvider>
                                </MissionDefinitionsProvider>
                            </InspectionsProvider>
                        </AlertProvider>
                    </AssetProvider>
                </SignalRProvider>
            </InstallationContext.Provider>
        </>
    )
}
