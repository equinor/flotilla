import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { SignInPage } from './components/SignInPage/SignInPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'

function App() {
    return (
        <div className="app-ui">
            <UnauthenticatedTemplate>
                <div className="sign-in-page">
                    <SignInPage></SignInPage>
                </div>
            </UnauthenticatedTemplate>
            <AuthenticatedTemplate>
                <FlotillaSite />
            </AuthenticatedTemplate>
        </div>
    )
}

export default App
