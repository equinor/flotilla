import { Button, Icon, Menu } from '@equinor/eds-core-react'
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
    const [isOpen, setIsOpen] = useState(false)
    const [anchorEl, setAnchorEl] = useState(null)
    const openMenu = () => {
        setIsOpen(true)
    }
    const closeMenu = () => {
        setIsOpen(false)
    }

    const paths = [
        { path: 'frontPage', label: 'Front page' },
        { path: 'missionControl', label: 'Mission control' },
        { path: 'history', label: 'Mission history' },
        { path: 'inspectionPage', label: 'Deck overview' },
        { path: 'robotsPage', label: 'Robots' },
    ]

    let navigate = useNavigate()
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
                    <Menu.Item onClick={() => routeChange(page.path)}>{TranslateText(page.label)}</Menu.Item>
                ))}
            </Menu>
        </>
    )
}
