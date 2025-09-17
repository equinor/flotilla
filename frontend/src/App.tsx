import { ApplicationInsights } from '@microsoft/applicationinsights-web'
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { AssetSelectionPage } from 'components/Pages/AssetSelectionPage/AssetSelectionPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'
import { MissionFilterProvider } from 'components/Contexts/MissionFilterContext'
import { MissionRunsProvider } from 'components/Contexts/MissionRunsContext'
import { AlertProvider } from 'components/Contexts/AlertContext'
import { AuthProvider } from 'components/Contexts/AuthProvider'
import { SignalRProvider } from 'components/Contexts/SignalRContext'
import { AssetProvider } from 'components/Contexts/RobotContext'
import { config } from 'config'
import { MissionDefinitionsProvider } from 'components/Contexts/MissionDefinitionsContext'
import { MediaStreamProvider } from 'components/Contexts/MediaStreamContext'
import { InspectionsProvider } from 'components/Contexts/InspectionsContext'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

const appInsights = new ApplicationInsights({
    config: {
        connectionString: config.AI_CONNECTION_STRING,
    },
})

if (config.AI_CONNECTION_STRING.length > 0) {
    appInsights.loadAppInsights()
    appInsights.trackPageView()
}

export const queryClient = new QueryClient()

const App = () => (
    <AuthProvider>
        <LanguageProvider>
            <SignalRProvider>
                <QueryClientProvider client={queryClient}>
                    <AssetProvider>
                        <InspectionsProvider>
                            <MissionDefinitionsProvider>
                                <MissionRunsProvider>
                                    <AlertProvider>
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
                                    </AlertProvider>
                                </MissionRunsProvider>
                            </MissionDefinitionsProvider>
                        </InspectionsProvider>
                    </AssetProvider>
                </QueryClientProvider>
            </SignalRProvider>
        </LanguageProvider>
    </AuthProvider>
)

export default App
