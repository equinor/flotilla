import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { AssetSelectionPage } from 'components/Pages/AssetSelectionPage/AssetSelectionPage'
import { FlotillaSite } from 'components/Pages/FlotillaSite'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { MissionControlProvider } from 'components/Contexts/MissionControlContext'
import { MissionFilterProvider } from 'components/Contexts/MissionFilterContext'
import { MissionsProvider } from 'components/Contexts/MissionListsContext'

function App() {
    return (
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
    )
}

export default App
