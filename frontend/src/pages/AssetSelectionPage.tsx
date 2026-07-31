import { useEffect, useState } from 'react'
import { Autocomplete, Button, CircularProgress, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import assetImage from 'mediaAssets/assetPage.jpg'
import { useNavigate } from 'react-router'
import { phone_width } from '../utils/constants'
import { Installation } from 'models/Installation'
import { useBackendApi } from 'api/UseBackendApi'

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
        <Header />
        <StyledContent>
            <InstallationPicker />
            <StyledImage src={assetImage} />
        </StyledContent>
    </>
)

const InstallationPicker = () => {
    const { TranslateText } = useLanguageContext()
    const backendApi = useBackendApi()
    const navigate = useNavigate()

    const [selectedInstallation, setSelectedInstallation] = useState<Installation | undefined>(undefined)
    const [installations, setInstallations] = useState<Installation[] | undefined>(undefined)
    const [isLoadingInstallations, setIsLoadingInstallations] = useState<boolean>(true)

    useEffect(() => {
        backendApi
            .getInstallations()
            .then((installations) => {
                setInstallations(installations)
                setIsLoadingInstallations(false)
            })
            .catch(() => {
                console.error(`Failed to retrieve list of installations`)
                setIsLoadingInstallations(false)
            })
    }, [backendApi])

    const handleClick = () => {
        navigate(selectedInstallation!.installationCode)
    }

    if (isLoadingInstallations) {
        return (
            <>
                <CircularProgress />
            </>
        )
    } else if (!installations) {
        return (
            <>
                <Typography>Not able to load installations. </Typography>
            </>
        )
    }

    return (
        <StyledAssetSelection>
            <Autocomplete
                options={installations.map((i) => i.name)}
                label=""
                dropdownHeight={200}
                placeholder={TranslateText('Select installation')}
                onOptionsChange={({ selectedItems }) => {
                    const selectedName = selectedItems[0]
                    const _selectedInstallation: Installation = installations.find((i) => i.name === selectedName)!
                    setSelectedInstallation(_selectedInstallation)
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
