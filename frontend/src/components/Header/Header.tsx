import { Button, Icon, Search, TopBar, Autocomplete } from '@equinor/eds-core-react'
import { accessible, account_circle, notifications } from '@equinor/eds-icons'
import { useAssetContext } from 'components/Contexts/AssetContext'
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
            <TopBar.Header>Flotilla - Robot Planner</TopBar.Header>
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
    const { asset, switchAsset } = useAssetContext();
    let savedAsset = sessionStorage.getItem('asset');
    let initialOption = ''
    if (savedAsset != null) {
        initialOption = savedAsset
        switchAsset(savedAsset)
    }

    const options = [
        "Test",
        "Kårstø",
        "Johan Sverdrup"
    ]
    return (
        <Autocomplete
            label="Select asset"
            options={options}
            initialSelectedOptions={[initialOption]}
            onOptionsChange={({ selectedItems }) => {
                switchAsset(selectedItems[0])
                console.log(selectedItems[0])
                console.log(asset)
            }}
        />
    )
}