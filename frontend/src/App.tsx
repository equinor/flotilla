import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { FlotillaSite } from 'pages/FlotillaSite'
import { LanguageProvider } from 'contexts/LanguageContext'
import { AuthProvider } from 'contexts/AuthProvider'
import { MediaStreamProvider } from 'contexts/MediaStreamContext'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

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
