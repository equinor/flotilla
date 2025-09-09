import { useEffect, useState } from 'react'
import { useIsAuthenticated } from '@azure/msal-react'
import { useMsal } from '@azure/msal-react'
import { loginRequest } from 'api/AuthConfig'
import { Autocomplete, Button, CircularProgress, Typography } from '@equinor/eds-core-react'
import { IPublicClientApplication } from '@azure/msal-browser'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import { config } from 'config'
import assetImage from 'mediaAssets/assetPage.jpg'
import { useNavigate } from 'react-router-dom'
import { phone_width } from '../../../utils/constants'
import { useRobotContext } from 'components/Contexts/RobotContext'

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
const StyledButton = styled(Button)`
    justify-content: center;
`
const StyledImage = styled.img`
    width: 100vw;
    object-fit: cover;
    height: 500px;

    @media (max-width: ${phone_width}) {
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
        return `${config.FRONTEND_BASE_ROUTE}/mission-control`
    } else {
        return `${config.FRONTEND_BASE_ROUTE}/front-page`
    }
}

const InstallationPicker = () => {
    const { installationName, switchInstallation, activeInstallations } = useRobotContext()
    const { TranslateText } = useLanguageContext()
    const [selectedInstallation, setSelectedInstallation] = useState<string>(installationName)
    const navigate = useNavigate()

    const handleClick = () => {
        switchInstallation(selectedInstallation)
        const target = findNavigationPage()
        navigate(target)
    }

    const installationNames = activeInstallations.map((i) => i.name)

    return (
        <StyledAssetSelection>
            <Autocomplete
                options={installationNames.sort()}
                label=""
                dropdownHeight={200}
                initialSelectedOptions={[selectedInstallation]}
                selectedOptions={[selectedInstallation]}
                placeholder={TranslateText('Select installation')}
                onOptionsChange={({ selectedItems }) => {
                    const selectedName = selectedItems[0]
                    setSelectedInstallation(selectedName ?? '')
                }}
                onInput={(e: React.ChangeEvent<HTMLInputElement>) => {
                    setSelectedInstallation(e.target.value ?? '')
                }}
                autoWidth={true}
                onFocus={(e) => e.preventDefault()}
            />
            <StyledButton onClick={() => handleClick()} disabled={!selectedInstallation}>
                {TranslateText('Confirm installation')}
            </StyledButton>
        </StyledAssetSelection>
    )
}
