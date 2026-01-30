import { useEffect, useState } from 'react'
import { Autocomplete, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import { config } from 'config'
import assetImage from 'mediaAssets/assetPage.jpg'
import { useNavigate } from 'react-router-dom'
import { phone_width } from '../../utils/constants'
import { useAssetContext } from 'components/Contexts/AssetContext'

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

export const AssetSelectionPage = () => (
    <>
        <Header page={'root'} />
        <StyledContent>
            <InstallationPicker />
            <StyledImage src={assetImage} />
        </StyledContent>
    </>
)

export const findNavigationPage = (installationCode: string) => {
    if (window.innerWidth <= 600) {
        return `${config.FRONTEND_BASE_ROUTE}/${installationCode}:mission-control`
    } else {
        return `${config.FRONTEND_BASE_ROUTE}/${installationCode}:front-page`
    }
}

const InstallationPicker = () => {
    const { installationName, switchInstallation, activeInstallations, installationCode } = useAssetContext()
    const { TranslateText } = useLanguageContext()
    const [selectedInstallation, setSelectedInstallation] = useState<string>(installationName)
    const [shouldNavigate, setShouldNavigate] = useState<boolean>(false)
    const navigate = useNavigate()

    const handleClick = () => {
        switchInstallation(selectedInstallation)
        setShouldNavigate(true)
    }

    useEffect(() => {
        if (shouldNavigate) {
            setShouldNavigate(false)
            const target = findNavigationPage(installationCode)
            navigate(target)
        }
    }, [shouldNavigate, installationCode])

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
                autoWidth={true}
                onFocus={(e) => e.preventDefault()}
            />
            <StyledButton onClick={() => handleClick()} disabled={!selectedInstallation}>
                {TranslateText('Confirm installation')}
            </StyledButton>
        </StyledAssetSelection>
    )
}
