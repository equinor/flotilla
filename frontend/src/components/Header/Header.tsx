import { Button, Icon, TopBar, Autocomplete } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { EchoPlantInfo } from 'models/EchoMission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'
import { SelectLanguageDialog } from './SelectLanguageDialog'
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

const StyledTopBarContent = styled(TopBar.CustomContent)`
    display: grid;
    grid-template-columns: minmax(50px, 265px) auto;
    align-items: end;
    gap: 0px 3rem;
`

export function Header() {
    return (
        <StyledTopBar>
            <TopBar.Header>Flotilla</TopBar.Header>
            <StyledTopBarContent>{AssetPicker()}</StyledTopBarContent>
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
                {SelectLanguageDialog()}
            </TopBar.Actions>
        </StyledTopBar>
    )
}

function AssetPicker() {
    const apiCaller = useApi()
    const [allPlantsMap, setAllPlantsMap] = useState<Map<string, string>>()
    const { asset, switchAsset } = useAssetContext()
    useEffect(() => {
        apiCaller.getEchoPlantInfo().then((response: EchoPlantInfo[]) => {
            const mapping = mapAssetCodeToName(response)
            setAllPlantsMap(mapping)
        })
    }, [])
    const mappedOptions = allPlantsMap ? allPlantsMap : new Map<string, string>()
    return (
        <Autocomplete
            options={Array.from(mappedOptions.keys()).sort()}
            label=""
            initialSelectedOptions={[asset]}
            placeholder={Text('Select asset')}
            onOptionsChange={({ selectedItems }) => {
                const mapKey = mappedOptions.get(selectedItems[0])
                if (mapKey != undefined) switchAsset(mapKey)
                else switchAsset('')
            }}
        />
    )
}

const mapAssetCodeToName = (echoPlantInfoArray: EchoPlantInfo[]): Map<string, string> => {
    var mapping = new Map<string, string>()
    echoPlantInfoArray.map((echoPlantInfo: EchoPlantInfo) => {
        mapping.set(echoPlantInfo.projectDescription, echoPlantInfo.installationCode)
    })
    return mapping
}
