import { ApplicationInsights } from '@microsoft/applicationinsights-web'
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { FlotillaSite } from 'pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { AuthProvider } from 'components/Contexts/AuthProvider'
import { config } from 'config'
import { MediaStreamProvider } from 'components/Contexts/MediaStreamContext'
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
                <QueryClientProvider client={queryClient}>
                    <MediaStreamProvider>
                        <FlotillaSite />
                    </MediaStreamProvider>
                </QueryClientProvider>
            </LanguageProvider>
        </AuthenticatedTemplate>
    </AuthProvider>
)

export default App
