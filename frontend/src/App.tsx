import { ApplicationInsights } from '@microsoft/applicationinsights-web'
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { AssetSelectionPage } from 'components/Pages/AssetSelectionPage/AssetSelectionPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'
import { MissionFilterProvider } from 'components/Contexts/MissionFilterContext'
import { MissionRunsProvider } from 'components/Contexts/MissionRunsContext'
import { AlertProvider } from 'components/Contexts/AlertContext'
import { InstallationProvider } from 'components/Contexts/InstallationContext'
import { AuthProvider } from 'components/Contexts/AuthProvider'
import { SignalRProvider } from 'components/Contexts/SignalRContext'
import { RobotProvider } from 'components/Contexts/RobotContext'
import { config } from 'config'
import { MissionDefinitionsProvider } from 'components/Contexts/MissionDefinitionsContext'
import { MediaStreamProvider } from 'components/Contexts/MediaStreamContext'
import { DockProvider } from 'components/Contexts/DockContext'
import { InspectionsProvider } from 'components/Contexts/InpectionsContext'

const appInsights = new ApplicationInsights({
    config: {
        connectionString: config.AI_CONNECTION_STRING,
    },
})

if (config.AI_CONNECTION_STRING.length > 0) {
    appInsights.loadAppInsights()
    appInsights.trackPageView()
}

const App = () => (
    <AuthProvider>
        <LanguageProvider>
            <SignalRProvider>
                <InstallationProvider>
                    <InspectionsProvider>
                        <MissionDefinitionsProvider>
                            <RobotProvider>
                                <MissionRunsProvider>
                                    <AlertProvider>
                                        <DockProvider>
                                            <MissionRunsProvider>
                                                <MissionControlProvider>
                                                    <UnauthenticatedTemplate>
                                                        <div className="sign-in-page">
                                                            <AssetSelectionPage></AssetSelectionPage>
                                                        </div>
                                                    </UnauthenticatedTemplate>
                                                    <AuthenticatedTemplate>
                                                        <MissionFilterProvider>
                                                            <MediaStreamProvider>
                                                                <FlotillaSite />
                                                            </MediaStreamProvider>
                                                        </MissionFilterProvider>
                                                    </AuthenticatedTemplate>
                                                </MissionControlProvider>
                                            </MissionRunsProvider>
                                        </DockProvider>
                                    </AlertProvider>
                                </MissionRunsProvider>
                            </RobotProvider>
                        </MissionDefinitionsProvider>
                    </InspectionsProvider>
                </InstallationProvider>
            </SignalRProvider>
        </LanguageProvider>
    </AuthProvider>
)

export default App
