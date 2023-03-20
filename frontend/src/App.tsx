import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { SignInPage } from './components/Pages/SignInPage/SignInPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { ErrorBoundary } from 'react-error-boundary'
import { ErrorFallback } from 'components/Pages/ErrorFallback'

function App() {
    return (
        // <ErrorBoundary fallbackRender={({ error, resetErrorBoundary }) => ErrorFallback(error)}>
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
        // </ErrorBoundary>
    )
}

export default App
