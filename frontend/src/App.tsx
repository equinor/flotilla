import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { SignInPage } from './components/Pages/SignInPage/SignInPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'

function App() {
    return (
        <LanguageProvider>
            <MissionControlProvider>
                {/* // <ErrorBoundary fallbackRender={({ error, resetErrorBoundary }) => ErrorFallback(error)}> */}
                <>
                    <UnauthenticatedTemplate>
                        <div className="sign-in-page">
                            <SignInPage></SignInPage>
                        </div>
                    </UnauthenticatedTemplate>
                    <AuthenticatedTemplate>
                        <FlotillaSite />
                    </AuthenticatedTemplate>
                </>
                {/* </ErrorBoundary> */}
            </MissionControlProvider>
        </LanguageProvider>
    )
}

export default App
