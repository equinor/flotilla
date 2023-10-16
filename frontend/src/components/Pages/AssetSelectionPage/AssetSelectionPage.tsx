import { useEffect, useState } from 'react'
import { useIsAuthenticated } from '@azure/msal-react'
import { useMsal } from '@azure/msal-react'
import { loginRequest } from '../../../api/AuthConfig'
import { Autocomplete, Button, TopBar, CircularProgress, Typography, Checkbox } from '@equinor/eds-core-react'
import { IPublicClientApplication } from '@azure/msal-browser'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { EchoPlantInfo } from 'models/EchoMission'
import { Header } from 'components/Header/Header'
import { config } from 'config'

const Centered = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
`

function handleLogin(instance: IPublicClientApplication) {
    instance.loginRedirect(loginRequest).catch((e) => {
        console.error(e)
    })
}
const StyledTopBarContent = styled(TopBar.CustomContent)`
    display: grid;
    grid-template-columns: minmax(50px, 265px) auto;
    gap: 0px 3rem;
    align-items: center;
`
const BlockLevelContainer = styled.div`
    & > * {
        display: block;
    }
`
const StyledCheckbox = styled(Checkbox)`
    margin-left: -14px;
`
const RowContainer = styled.div`
    display: flex;
    flex-direction: row;
    align-items: flex-start;
    justify-content: flex-start;
    margin-top: 50px;
`

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
                    <Centered>
                        <RowContainer>
                            <StyledTopBarContent>
                                <InstallationPicker />
                            </StyledTopBarContent>
                        </RowContainer>
                        {/* TODO! ADD image here*/}
                    </Centered>
                </>
            ) : (
                <Centered>
                    <Typography variant="body_long_bold" color="primary">
                        Authentication
                    </Typography>
                    <CircularProgress size={48} color="primary" />
                </Centered>
            )}
        </>
    )
}

function InstallationPicker() {
    const { installationName, switchInstallation } = useInstallationContext()
    const { TranslateText } = useLanguageContext()
    const [allPlantsMap, setAllPlantsMap] = useState<Map<string, string>>(new Map())
    const [selectedInstallation, setSelectedInstallation] = useState<string>(installationName)
    const [showActivePlants, setShowActivePlants] = useState<boolean>(true)
    const [updateListOfActivePlants, setUpdateListOfActivePlants] = useState<boolean>(false)

    const mappedOptions = allPlantsMap ? allPlantsMap : new Map<string, string>()

    const validateInstallation = (installationName: string) =>
        Array.from(mappedOptions.keys()).includes(installationName)

    useEffect(() => {
        const plantPromise = showActivePlants ? BackendAPICaller.getActivePlants() : BackendAPICaller.getEchoPlantInfo()
        plantPromise.then(async (response: EchoPlantInfo[]) => {
            const mapping = mapInstallationCodeToName(response)
            setAllPlantsMap(mapping)
        })
    }, [showActivePlants, updateListOfActivePlants])

    return (
        <>
            <BlockLevelContainer>
                <Autocomplete
                    options={Array.from(mappedOptions.keys()).sort()}
                    label=""
                    initialSelectedOptions={[selectedInstallation]}
                    selectedOptions={[selectedInstallation]}
                    placeholder={TranslateText('Select installation')}
                    onOptionsChange={({ selectedItems }) => {
                        const selectedName = selectedItems[0]
                        setSelectedInstallation(selectedName)
                    }}
                    onInput={(e: React.ChangeEvent<HTMLInputElement>) => {
                        setSelectedInstallation(e.target.value ?? '')
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
            </BlockLevelContainer>
            <Button
                onClick={() => switchInstallation(selectedInstallation)}
                disabled={!validateInstallation(selectedInstallation)}
                href={`${config.FRONTEND_BASE_ROUTE}/FrontPage`}
            >
                {TranslateText('Confirm installation')}
            </Button>
        </>
    )
}

const mapInstallationCodeToName = (echoPlantInfoArray: EchoPlantInfo[]): Map<string, string> => {
    var mapping = new Map<string, string>()
    echoPlantInfoArray.forEach((echoPlantInfo: EchoPlantInfo) => {
        mapping.set(echoPlantInfo.projectDescription, echoPlantInfo.plantCode)
    })
    return mapping
}
