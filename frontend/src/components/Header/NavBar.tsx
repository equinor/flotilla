import { Button, Icon, Menu, Tabs } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useContext, useState } from 'react'
import { Link, matchPath, useNavigate } from 'react-router-dom'
import { styled } from 'styled-components'
import { Icons } from 'utils/icons'
import { phone_width } from 'utils/constants'
import { InstallationContext } from 'components/Contexts/InstallationContext'

const StyledButton = styled(Button)`
    width: 100px;
    border-radius: 4px;
`

const NavBarAsButton = () => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const [isOpen, setIsOpen] = useState(false)
    const [anchorEl, setAnchorEl] = useState(null)
    const navigate = useNavigate()

    const openMenu = () => {
        setIsOpen(true)
    }
    const closeMenu = () => {
        setIsOpen(false)
    }

    return (
        <>
            <StyledButton
                id="menu"
                variant="ghost"
                ref={setAnchorEl}
                aria-label="Menu"
                aria-haspopup="true"
                aria-expanded={isOpen}
                aria-controls="menu"
                onClick={() => (isOpen ? closeMenu() : openMenu())}
            >
                <Icon name={Icons.Menu} />
                {TranslateText('Menu')}
            </StyledButton>
            <Menu open={isOpen} id="menu" aria-labelledby="menu" onClose={closeMenu} anchorEl={anchorEl}>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/mission-control`)}>
                    {TranslateText('Mission Control')}
                </Menu.Item>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/inspection-overview`)}>
                    {TranslateText('Area Overview')}
                </Menu.Item>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/predefined-missions`)}>
                    {TranslateText('Predefined Missions')}
                </Menu.Item>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/history`)}>
                    {TranslateText('Mission History')}
                </Menu.Item>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/auto-schedule`)}>
                    {TranslateText('Auto Scheduling')}
                </Menu.Item>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/robots`)}>
                    {TranslateText('Robots')}
                </Menu.Item>
            </Menu>
        </>
    )
}

function useRouteMatch(patterns: readonly string[]) {
    for (let i = 0; i < patterns.length; i += 1) {
        const pattern = patterns[i]
        const possibleMatch = matchPath(pattern, location.pathname)
        if (possibleMatch !== null) {
            return possibleMatch
        }
    }
    return null
}

const NavBarAsTabs = () => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const routeMatch = useRouteMatch([
        '/:installationCode/mission-control',
        '/:installationCode/inspection-overview',
        '/:installationCode/predefined-missions',
        '/:installationCode/history',
        '/:installationCode/auto-schedule',
        '/:installationCode/robots',
    ])
    const currentPath = routeMatch?.pattern?.path

    return (
        <>
            <Tabs activeTab={currentPath}>
                <Tabs.List>
                    <Tabs.Tab
                        value="/:installationCode/mission-control"
                        to={`/${installation.installationCode}/mission-control`}
                        as={Link}
                    >
                        {TranslateText('Mission Control')}
                    </Tabs.Tab>
                    <Tabs.Tab
                        value="/:installationCode/inspection-overview"
                        to={`/${installation.installationCode}/inspection-overview`}
                        as={Link}
                    >
                        {TranslateText('Area Overview')}
                    </Tabs.Tab>
                    <Tabs.Tab
                        value="/:installationCode/predefined-missions"
                        to={`/${installation.installationCode}/predefined-missions`}
                        as={Link}
                    >
                        {TranslateText('Predefined Missions')}
                    </Tabs.Tab>
                    <Tabs.Tab
                        value="/:installationCode/history"
                        to={`/${installation.installationCode}/history`}
                        as={Link}
                    >
                        {TranslateText('Mission History')}
                    </Tabs.Tab>
                    <Tabs.Tab
                        value="/:installationCode/auto-schedule"
                        to={`/${installation.installationCode}/auto-schedule`}
                        as={Link}
                    >
                        {TranslateText('Auto Scheduling')}
                    </Tabs.Tab>
                    <Tabs.Tab
                        value="/:installationCode/robots"
                        to={`/${installation.installationCode}/robots`}
                        as={Link}
                    >
                        {TranslateText('Robots')}
                    </Tabs.Tab>
                </Tabs.List>
            </Tabs>
        </>
    )
}

const StyledShowOnMobile = styled.div`
    display: none;
    @media (max-width: ${phone_width}) {
        display: block;
    }
`

const StyledShowOffMobile = styled.div`
    display: block;
    @media (max-width: ${phone_width}) {
        display: none;
    }
`

export const NavBar = () => {
    return (
        <>
            <StyledShowOnMobile>
                <NavBarAsButton />
            </StyledShowOnMobile>
            <StyledShowOffMobile>
                <NavBarAsTabs />
            </StyledShowOffMobile>
        </>
    )
}
