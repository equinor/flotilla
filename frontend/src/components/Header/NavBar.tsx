import { Button, Icon, Menu, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useContext, useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { styled } from 'styled-components'
import { Icons } from 'utils/icons'
import { phone_width } from 'utils/constants'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { StopRobotDialog } from 'pages/FrontPage/MissionOverview/StopDialogs'
import { useAssetContext } from 'components/Contexts/AssetContext'

const StyledButton = styled(Button)`
    width: 100px;
    border-radius: 4px;
`
const NavWrapper = styled.div`
    background: ${tokens.colors.ui.background__default.hex};
    border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
    padding: 0 1rem;
    display: flex;
    justify-content: space-between;
    align-items: stretch;
`
const NavLinks = styled.nav`
    display: flex;
    align-items: stretch;
`
const NavItem = styled(NavLink)`
    display: flex;
    align-items: center;
    padding: 0 14px;
    height: 48px;
    font-family: Equinor, sans-serif;
    font-size: 0.78rem;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__default.hex};
    border-bottom: 3px solid transparent;
    text-decoration: none;
    transition:
        color 0.15s ease,
        border-color 0.15s ease;
    white-space: nowrap;
    &.active {
        font-weight: 700;
        color: ${tokens.colors.interactive.primary__resting.hex};
        border-bottom-color: ${tokens.colors.interactive.primary__resting.hex};
    }
    &:hover {
        color: ${tokens.colors.interactive.primary__resting.hex};
        border-bottom-color: ${tokens.colors.interactive.primary__hover_alt.hex};
    }
`
const RightContent = styled.div`
    display: flex;
    align-items: center;
    gap: 24px;
`

const NavBarAsButton = () => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()
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
                {installationInspectionAreas.length > 1 && (
                    <Menu.Item onClick={() => navigate(`/${installation.installationCode}/inspection-overview`)}>
                        {TranslateText('Area Overview')}
                    </Menu.Item>
                )}
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/predefined-missions`)}>
                    {TranslateText('Predefined Missions')}
                </Menu.Item>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/history`)}>
                    {TranslateText('Mission History')}
                </Menu.Item>
                <Menu.Item onClick={() => navigate(`/${installation.installationCode}/auto-schedule`)}>
                    {TranslateText('Auto Scheduling')}
                </Menu.Item>
            </Menu>
        </>
    )
}

const StyledOngoingMissionsInfo = styled.div`
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 4px 12px;
    border-radius: 20px;
    background: ${tokens.colors.ui.background__light.hex};
    cursor: pointer;
    white-space: nowrap;
    transition: background 0.15s ease;
    &:hover {
        background: ${tokens.colors.interactive.primary__hover_alt.hex};
    }
`
const OngoingDot = styled.span<{ $active: boolean }>`
    width: 8px;
    height: 8px;
    border-radius: 50%;
    flex-shrink: 0;
    background: ${({ $active }) =>
        $active ? tokens.colors.interactive.primary__resting.hex : tokens.colors.text.static_icons__tertiary.hex};
`
const OngoingMissionsInfo = ({ goToOngoingTab }: { goToOngoingTab: () => void }) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions } = useMissionsContext()

    return (
        <StyledOngoingMissionsInfo onClick={goToOngoingTab}>
            <OngoingDot $active={ongoingMissions.length > 0} />
            <Typography variant="body_short">
                {`${ongoingMissions.length} ${TranslateText('Ongoing missions').toLowerCase()}`}
            </Typography>
        </StyledOngoingMissionsInfo>
    )
}

const NavBarAsTabs = () => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()
    const navigate = useNavigate()

    const navItems = [
        { to: `/${installation.installationCode}/mission-control`, label: TranslateText('Mission Control') },
        ...(installationInspectionAreas.length <= 1
            ? []
            : [{ to: `/${installation.installationCode}/inspection-overview`, label: TranslateText('Area Overview') }]),
        { to: `/${installation.installationCode}/predefined-missions`, label: TranslateText('Predefined Missions') },
        { to: `/${installation.installationCode}/history`, label: TranslateText('Mission History') },
        { to: `/${installation.installationCode}/auto-schedule`, label: TranslateText('Auto Scheduling') },
        { to: `/${installation.installationCode}/data-overview`, label: TranslateText('Data Overview') },
    ]

    return (
        <NavWrapper>
            <NavLinks>
                {navItems.map(({ to, label }) => (
                    <NavItem key={to} to={to} end>
                        {label}
                    </NavItem>
                ))}
            </NavLinks>
            <RightContent>
                <OngoingMissionsInfo
                    goToOngoingTab={() => {
                        navigate(`/${installation.installationCode}`)
                    }}
                />
                <StopRobotDialog />
            </RightContent>
        </NavWrapper>
    )
}

const StyledShowOnMobile = styled.div`
    display: none;
    @media (max-width: ${phone_width}) {
        display: block;
    }
`

const StyledShowOffMobile = styled.div`
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
