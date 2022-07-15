import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { SignInPage } from './components/SignInPage/SignInPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { AssetProvider } from 'components/Contexts/AssetContext'

function App() {
    return (
        <div>
            <UnauthenticatedTemplate>
                <div className="sign-in-page">
                    <SignInPage></SignInPage>
                </div>
            </UnauthenticatedTemplate>
            <AuthenticatedTemplate>
                <AssetProvider>
                    <FlotillaSite />
                </AssetProvider>
            </AuthenticatedTemplate>
        </div>
    )
}

export default App
