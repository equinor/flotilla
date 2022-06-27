import { Button, Icon, Search, TopBar } from "@equinor/eds-core-react"
import { accessible, account_circle, notifications } from "@equinor/eds-icons"
import styled from "styled-components"

Icon.add({account_circle, accessible, notifications})

const Icons = styled.div`
    display: flex;
    align-items: center;
    flex-direction: row-reverse;
    > * {
        margin-left: 40px;
    }
`
export function Header() {
    return (
        <TopBar>
            <TopBar.Header>
                Flotilla - Robot Planner
            </TopBar.Header>
            <TopBar.CustomContent>
                <Search aria-label="sitewide" id="search-normal" placeholder="Search"/>
            </TopBar.CustomContent>
            <TopBar.Actions>
                <Icons>
                    <Button variant="ghost_icon" onClick={() => console.log("Clicked account icon")}>
                        <Icon name="account_circle" size = {16} title="user"/>
                    </Button>
                    <Button variant="ghost_icon" onClick={() => console.log("Clicked accessibility icon")}>
                        <Icon name="accessible" size = {16}/>
                    </Button>
                    <Button variant="ghost_icon" onClick={() => console.log("Clicked notification icon")}>
                        <Icon name="notifications" size={16}/>
                    </Button>
                </Icons>
            </TopBar.Actions>

        </TopBar>
    )
}