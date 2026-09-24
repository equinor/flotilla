import { useEffect, useState } from 'react'
import { Autocomplete, Button, Card, CircularProgress, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import assetImage from 'mediaAssets/assetPage.jpg'
import { useNavigate } from 'react-router'
import { phone_width } from '../utils/constants'
import { Installation } from 'models/Installation'
import { useBackendApi } from 'api/UseBackendApi'
import { cardShadow, PageContent, PageBackground } from 'components/Styles/StyledComponents'

const StyledAssetSelection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 4px;
`
const StyledButton = styled(Button)`
    justify-content: center;
`
const StyledImage = styled.img`
    width: 100%;
    object-fit: cover;
    height: 500px;

    @media (max-width: ${phone_width}) {
        height: 400px;
    }
`
const AssetPageBackground = styled(PageBackground)`
    padding-top: 80px;
    gap: 80px;
`
// EDS Card is width: 100%; width: auto makes it size to its content.
const StyledPickerCard = styled(Card)`
    width: auto;
    align-self: center;
    padding: 2rem;
    gap: 1rem;
    box-shadow: ${cardShadow};
`

export const AssetSelectionPage = () => (
    <>
        <Header />
        <AssetPageBackground>
            <PageContent>
                <StyledPickerCard>
                    <InstallationPicker />
                </StyledPickerCard>
            </PageContent>
            <StyledImage src={assetImage} />
        </AssetPageBackground>
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
