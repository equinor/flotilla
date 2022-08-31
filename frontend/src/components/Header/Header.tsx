import { Button, Icon, Search, TopBar, Autocomplete } from '@equinor/eds-core-react'
import { accessible, account_circle, notifications } from '@equinor/eds-icons'
import { useApi } from 'api/ApiCaller'
import { assetOptions, useAssetContext } from 'components/Contexts/AssetContext'
import { EchoPlantInfo } from 'models/EchoPlantInfo'
import { useEffect, useState } from 'react'
import styled from 'styled-components'

Icon.add({ account_circle, accessible, notifications })

const StyledTopBar = styled(TopBar)`
    margin-bottom: 2rem;
`

const Icons = styled.div`
    display: flex;
    align-items: center;
    flex-direction: row-reverse;
    > * {
        margin-left: 40px;
    }
`

const StyledTopBarContent = styled(TopBar.CustomContent)`
    display: grid;
    grid-template-columns: minmax(50px, 200px) auto;
    align-items: end;
    gap: 0px 3rem;
`

export function Header() {
    return (
        <StyledTopBar>
            <TopBar.Header>Flotilla</TopBar.Header>
            <StyledTopBarContent>
                {AssetPicker()}

                <Search aria-label="sitewide" id="search-normal" placeholder="Search" />
            </StyledTopBarContent>
            <TopBar.Actions>
                <Icons>
                    <Button variant="ghost_icon" onClick={() => console.log('Clicked account icon')}>
                        <Icon name="account_circle" size={16} title="user" />
                    </Button>
                    <Button variant="ghost_icon" onClick={() => console.log('Clicked accessibility icon')}>
                        <Icon name="accessible" size={16} />
                    </Button>
                    <Button variant="ghost_icon" onClick={() => console.log('Clicked notification icon')}>
                        <Icon name="notifications" size={16} />
                    </Button>
                </Icons>
            </TopBar.Actions>
        </StyledTopBar>
    )
}

function AssetPicker() {
    const apiCaller = useApi();
    const [allPlantsMap, setAllPlantsMap] = useState<Map<string, string>>();
    const { asset, switchAsset } = useAssetContext()
    useEffect(() => {
        apiCaller.getEchoPlantInfo().then((response: EchoPlantInfo[]) => {
            var temporaryMap = new Map<string, string>()
            response.map((echoPlantInfo: EchoPlantInfo) => {
                temporaryMap.set(echoPlantInfo.projectDescription, echoPlantInfo.installationCode)
            })
            setAllPlantsMap(temporaryMap);
        });
    }, [])
    let savedAsset = sessionStorage.getItem('assetString')
    let initialOption = ''
    if (savedAsset != null) {
        initialOption = savedAsset
        switchAsset(savedAsset)
    }
    const mappedOptions = allPlantsMap ? allPlantsMap : new Map<string, string>()
    return (
        <Autocomplete
            options={Array.from(mappedOptions.keys())}
            label=""
            initialSelectedOptions={[initialOption]}
            placeholder="Select asset"
            onOptionsChange={({ selectedItems }) => {
                const mapKey = mappedOptions.get(selectedItems[0])
                if (mapKey != undefined)
                    switchAsset(mapKey)
                else
                    switchAsset("")
            }}
        />
    )
}