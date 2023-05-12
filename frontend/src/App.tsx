import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { SignInPage } from './components/Pages/SignInPage/SignInPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'

function App() {
    return (
        <LanguageProvider>
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
        </LanguageProvider>
    )
}

export default App
