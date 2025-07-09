import { useEffect, useState } from 'react'
import { useIsAuthenticated } from '@azure/msal-react'
import { useMsal } from '@azure/msal-react'
import { loginRequest } from 'api/AuthConfig'
import { Autocomplete, Button, CircularProgress, Typography, Checkbox } from '@equinor/eds-core-react'
import { IPublicClientApplication } from '@azure/msal-browser'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { PlantInfo } from 'models/MissionDefinition'
import { Header } from 'components/Header/Header'
import { config } from 'config'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import assetImage from 'mediaAssets/assetPage.jpg'

const Centered = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 10px;
`
const StyledAssetSelection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 4px;
`
const StyledCheckbox = styled(Checkbox)`
    padding-right: 14px;
`
const StyledButton = styled(Button)`
    justify-content: center;
`
const StyledImage = styled.img`
    width: 100vw;
    object-fit: cover;
    height: 500px;

    @media (max-width: 500px) {
        height: 400px;
    }
`
const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 80px;
    gap: 80px;
`

const handleLogin = (instance: IPublicClientApplication) => {
    instance.loginRedirect(loginRequest).catch((e) => {
        console.error(e)
    })
}

export const AssetSelectionPage = () => {
    const isAuthenticated = useIsAuthenticated()
    const { instance } = useMsal()

    useEffect(() => {
        if (!isAuthenticated) {
            handleLogin(instance)
        }
    }, [isAuthenticated, instance])

    return (
        <>
            {isAuthenticated ? (
                <>
                    <Header page={'root'} />
                    <StyledContent>
                        <InstallationPicker />
                        <StyledImage src={assetImage} />
                    </StyledContent>
                </>
            ) : (
                <Centered>
                    <Typography variant="body_long_bold" color="primary">
                        Authentication
                    </Typography>
                    <CircularProgress size={48} />
                </Centered>
            )}
        </>
    )
}

export const findNavigationPage = () => {
    if (window.innerWidth <= 600) {
        return `${config.FRONTEND_BASE_ROUTE}/missionControl`
    } else {
        return `${config.FRONTEND_BASE_ROUTE}/FrontPage`
    }
}

const InstallationPicker = () => {
    const { installationName, switchInstallation } = useInstallationContext()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [allPlantsMap, setAllPlantsMap] = useState<Map<string, string>>(new Map())
    const [selectedInstallation, setSelectedInstallation] = useState<string>(installationName)
    const [showActivePlants, setShowActivePlants] = useState<boolean>(true)
    const [updateListOfActivePlants, setUpdateListOfActivePlants] = useState<boolean>(false)

    const mappedOptions = allPlantsMap ? allPlantsMap : new Map<string, string>()

    const validateInstallation = (installationName: string) =>
        Array.from(mappedOptions.keys()).includes(installationName)

    useEffect(() => {
        if (BackendAPICaller.accessToken) {
            const plantPromise = showActivePlants ? BackendAPICaller.getActivePlants() : BackendAPICaller.getPlantInfo()
            plantPromise
                .then(async (response: PlantInfo[]) => {
                    const mapping = mapInstallationCodeToName(response)
                    setAllPlantsMap(mapping)
                })
                .catch(() => {
                    setAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertContent
                            translatedMessage={TranslateText('Failed to retrieve installations')}
                        />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertListContent
                            translatedMessage={TranslateText('Failed to retrieve installations from Echo')}
                        />,
                        AlertCategory.ERROR
                    )
                })
        }
    }, [showActivePlants, updateListOfActivePlants])

    return (
        <StyledAssetSelection>
            <Autocomplete
                options={Array.from(mappedOptions.keys()).sort()}
                label=""
                dropdownHeight={200}
                initialSelectedOptions={[selectedInstallation]}
                selectedOptions={[selectedInstallation]}
                placeholder={TranslateText('Select installation')}
                onOptionsChange={({ selectedItems }) => {
                    const selectedName = selectedItems[0]
                    setSelectedInstallation(validateInstallation(selectedName) ? selectedName : '')
                }}
                onInput={(e: React.ChangeEvent<HTMLInputElement>) => {
                    setSelectedInstallation(validateInstallation(e.target.value) ? e.target.value : '')
                }}
                autoWidth={true}
                onFocus={(e) => {
                    e.preventDefault()
                    setUpdateListOfActivePlants(!updateListOfActivePlants)
                }}
            />
            <StyledCheckbox
                label={TranslateText('Show only active installations')}
                checked={showActivePlants}
                onChange={(e) => setShowActivePlants(e.target.checked)}
            />
            <StyledButton
                onClick={() => switchInstallation(selectedInstallation)}
                disabled={!selectedInstallation}
                href={findNavigationPage()}
            >
                {TranslateText('Confirm installation')}
            </StyledButton>
        </StyledAssetSelection>
    )
}

const mapInstallationCodeToName = (plantInfoArray: PlantInfo[]): Map<string, string> => {
    const mapping = new Map<string, string>()
    plantInfoArray.forEach((plantInfo: PlantInfo) => {
        mapping.set(plantInfo.projectDescription, plantInfo.plantCode)
    })
    return mapping
}
