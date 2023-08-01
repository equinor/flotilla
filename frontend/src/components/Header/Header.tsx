import { config } from 'config'
import { Button, Icon, TopBar, Autocomplete, Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { usePlantContext } from 'components/Contexts/PlantContext'
import { EchoPlantInfo } from 'models/EchoMission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { SelectLanguage } from './LanguageSelector'
import { Icons } from 'utils/icons'

const StyledTopBar = styled(TopBar)`
    margin-bottom: 2rem;
`

const IconStyle = styled.div`
    display: flex;
    align-items: center;
    flex-direction: row-reverse;
    > * {
        margin-left: 1rem;
    }
`

const HandPointer = styled.div`
    cursor: pointer;
`

const StyledTopBarContent = styled(TopBar.CustomContent)`
    display: grid;
    grid-template-columns: minmax(50px, 265px) auto;
    align-items: end;
    gap: 0px 3rem;
`

const SelectLanguageWrapper = styled.div`
    margin-left: 1.5rem;
`

export function Header({ page }: { page: string }) {
    return (
        <StyledTopBar>
            <HandPointer>
                <TopBar.Header
                    onClick={() => {
                        window.location.href = `${config.FRONTEND_URL}/`
                    }}
                >
                    <Typography variant="body_long_bold" color="primary">
                        Flotilla
                    </Typography>
                </TopBar.Header>
            </HandPointer>
            <StyledTopBarContent>{InstallationPicker(page)}</StyledTopBarContent>
            <TopBar.Actions>
                <IconStyle>
                    <Button variant="ghost_icon" onClick={() => console.log('Clicked account icon')}>
                        <Icon name={Icons.Account} size={16} title="user" />
                    </Button>
                    <Button variant="ghost_icon" onClick={() => console.log('Clicked accessibility icon')}>
                        <Icon name={Icons.Accessible} size={16} />
                    </Button>
                    <Button variant="ghost_icon" onClick={() => console.log('Clicked notification icon')}>
                        <Icon name={Icons.Notifications} size={16} />
                    </Button>
                </IconStyle>
                <SelectLanguageWrapper>{SelectLanguage()}</SelectLanguageWrapper>
            </TopBar.Actions>
        </StyledTopBar>
    )
}

function InstallationPicker(page: string) {
    const { TranslateText } = useLanguageContext()
    const [allPlantsMap, setAllPlantsMap] = useState<{ [plantDescription: string]: EchoPlantInfo }>()
    const { currentPlant, setPlant } = usePlantContext()
    useEffect(() => {
        BackendAPICaller.getEchoPlantInfo().then((response: EchoPlantInfo[]) => {
            const mapping = mapInstallationCodeToName(response)
            setAllPlantsMap(mapping)
        })
    }, [])
    const emptyObject: { [plantDescription: string]: EchoPlantInfo } = {}
    const mappedOptions = allPlantsMap ? allPlantsMap : emptyObject
    return (
        <Autocomplete
            options={Array.from(Object.keys(mappedOptions)).sort()}
            label=""
            disabled={page === 'mission'}
            initialSelectedOptions={[currentPlant?.projectDescription.toUpperCase()]}
            placeholder={TranslateText('Select installation')}
            onOptionsChange={({ selectedItems }) => {
                if (!selectedItems[0]) {
                    setPlant(undefined)
                } else {
                    setPlant(mappedOptions[selectedItems[0]])
                }
            }}
        />
    )
}

const mapInstallationCodeToName = (
    echoPlantInfoArray: EchoPlantInfo[]
): { [plantDescription: string]: EchoPlantInfo } => {
    var mapping: { [plantDescription: string]: EchoPlantInfo } = {}
    echoPlantInfoArray.forEach((echoPlantInfo: EchoPlantInfo) => {
        mapping[echoPlantInfo.projectDescription] = echoPlantInfo
    })
    return mapping
}
