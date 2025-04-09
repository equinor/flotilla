import { Button, Icon, Menu } from '@equinor/eds-core-react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { config } from 'config'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { styled } from 'styled-components'
import { Icons } from 'utils/icons'

const StyledButton = styled(Button)`
    width: 100px;
    border-radius: 4px;
`

export const NavigationMenu = () => {
    const { TranslateText } = useLanguageContext()
    const { installationInspectionAreas } = useInstallationContext()
    const [isOpen, setIsOpen] = useState(false)
    const [anchorEl, setAnchorEl] = useState(null)
    const openMenu = () => {
        setIsOpen(true)
    }
    const closeMenu = () => {
        setIsOpen(false)
    }

    const paths =
        installationInspectionAreas.length > 1
            ? [
                  { path: 'missionControl', label: 'Mission Control' },
                  { path: 'history', label: 'Mission History' },
                  { path: 'inspectionOverview', label: 'Deck Overview' },
                  { path: 'predefinedMissions', label: 'Predefined Missions' },
                  { path: 'autoSchedule', label: 'Auto Scheduling' },
                  { path: 'robots', label: 'Robots' },
              ]
            : [
                  { path: 'missionControl', label: 'Mission Control' },
                  { path: 'history', label: 'Mission History' },
                  { path: 'predefinedMissions', label: 'Predefined Missions' },
                  { path: 'autoSchedule', label: 'Auto Scheduling' },
                  { path: 'robots', label: 'Robots' },
              ]

    const navigate = useNavigate()
    const routeChange = (routePath: string) => {
        const path = `${config.FRONTEND_BASE_ROUTE}/${routePath}`
        navigate(path)
        return
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
                {paths.map((page) => (
                    <Menu.Item key={page.label} onClick={() => routeChange(page.path)}>
                        {TranslateText(page.label)}
                    </Menu.Item>
                ))}
            </Menu>
        </>
    )
}
