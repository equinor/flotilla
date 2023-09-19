import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { AssetSelectionPage } from 'components/Pages/AssetSelectionPage/AssetSelectionPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'
import { MissionFilterProvider } from 'components/Contexts/MissionFilterContext'
import { MissionsProvider } from 'components/Contexts/MissionListsContext'
import { SafeZoneProvider } from 'components/Contexts/SafeZoneContext'

function App() {
    return (
        <SafeZoneProvider>
            <MissionsProvider>
                <LanguageProvider>
                    <MissionControlProvider>
                        <>
                            <UnauthenticatedTemplate>
                                <div className="sign-in-page">
                                    <AssetSelectionPage></AssetSelectionPage>
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
            </MissionsProvider>
        </SafeZoneProvider>
    )
}

export default App
