import { ApplicationInsights } from '@microsoft/applicationinsights-web'
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { FlotillaSite } from 'pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'
import { MissionRunsProvider } from 'components/Contexts/MissionRunsContext'
import { AlertProvider } from 'components/Contexts/AlertContext'
import { AuthProvider } from 'components/Contexts/AuthProvider'
import { SignalRProvider } from 'components/Contexts/SignalRContext'
import { AssetProvider } from 'components/Contexts/AssetContext'
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
        <UnauthenticatedTemplate />
        <AuthenticatedTemplate>
            <LanguageProvider>
                <SignalRProvider>
                    <QueryClientProvider client={queryClient}>
                        <AssetProvider>
                            <InspectionsProvider>
                                <MissionDefinitionsProvider>
                                    <MissionRunsProvider>
                                        <AlertProvider>
                                            <MissionControlProvider>
                                                <MediaStreamProvider>
                                                    <FlotillaSite />
                                                </MediaStreamProvider>
                                            </MissionControlProvider>
                                        </AlertProvider>
                                    </MissionRunsProvider>
                                </MissionDefinitionsProvider>
                            </InspectionsProvider>
                        </AssetProvider>
                    </QueryClientProvider>
                </SignalRProvider>
            </LanguageProvider>
        </AuthenticatedTemplate>
    </AuthProvider>
)

export default App
