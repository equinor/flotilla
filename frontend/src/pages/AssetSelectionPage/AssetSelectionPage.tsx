import { useState } from 'react'
import { Autocomplete, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import { config } from 'config'
import assetImage from 'mediaAssets/assetPage.jpg'
import { useNavigate } from 'react-router-dom'
import { phone_width } from '../../utils/constants'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { Installation } from 'models/Installation'

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
    const { installationName, switchInstallation, activeInstallations } = useAssetContext()
    const { TranslateText } = useLanguageContext()
    const [selectedInstallationName, setSelectedInstallationName] = useState<string>(installationName)
    const navigate = useNavigate()

    const handleClick = () => {
        const selectedInstallation: Installation | undefined = activeInstallations.find(
            (i) => i.name === selectedInstallationName
        )
        if (selectedInstallation) {
            switchInstallation(selectedInstallation.id)
            const target = findNavigationPage(selectedInstallation.installationCode)
            navigate(target)
        }
    }

    const installationNames = activeInstallations.map((i) => i.name)

    return (
        <StyledAssetSelection>
            <Autocomplete
                options={installationNames}
                label=""
                dropdownHeight={200}
                initialSelectedOptions={[selectedInstallationName]}
                selectedOptions={[selectedInstallationName]}
                placeholder={TranslateText('Select installation')}
                onOptionsChange={({ selectedItems }) => {
                    const selectedName = selectedItems[0]
                    setSelectedInstallationName(selectedName ?? '')
                }}
                autoWidth={true}
                onFocus={(e) => e.preventDefault()}
            />
            <StyledButton onClick={() => handleClick()} disabled={!selectedInstallationName}>
                {TranslateText('Confirm installation')}
            </StyledButton>
        </StyledAssetSelection>
    )
}
