import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { SignInPage } from './components/Pages/SignInPage/SignInPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'
import { MissionFilterProvider } from 'components/Contexts/MissionFilterContext'
import { MissionQueueProvider } from 'components/Contexts/MissionQueueContext'
import { SafeZoneProvider } from 'components/Contexts/SafeZoneContext'

function App() {
    return (
        <SafeZoneProvider>
            <MissionQueueProvider>
                <LanguageProvider>
                    <MissionControlProvider>
                        <>
                            <UnauthenticatedTemplate>
                                <div className="sign-in-page">
                                    <SignInPage></SignInPage>
                                </div>
                            </UnauthenticatedTemplate>
                            <AuthenticatedTemplate>
                                <MissionFilterProvider>
                                    <FlotillaSite />
                                </MissionFilterProvider>
                            </AuthenticatedTemplate>
                        </>
                    </MissionControlProvider>
                </LanguageProvider>
            </MissionQueueProvider>
        </SafeZoneProvider>
    )
}

export default App
