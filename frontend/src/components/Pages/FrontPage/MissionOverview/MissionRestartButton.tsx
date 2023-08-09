import { Mission } from 'models/Mission'
import { Button, EdsProvider, Icon, Menu, Tooltip } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { BackendAPICaller } from 'api/ApiCaller'
import { config } from 'config'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useRef, useState } from 'react'

const Centered = styled.div`
    display: flex;
    align-content: center;
    align-items: center;
`

interface MissionProps {
    mission: Mission
}

export enum ReRunOptions {
    ReRun,
    ReRunFailed,
}

export function MissionRestartButton({ mission }: MissionProps) {
    const { TranslateText } = useLanguageContext()
    const [isOpen, setIsOpen] = useState<boolean>(false)
    const anchorRef = useRef<HTMLButtonElement>(null)
    const openMenu = () => {
        setIsOpen(true)
    }
    const closeMenu = () => {
        setIsOpen(false)
    }

    let navigate = useNavigate()
    const navigateToHome = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/`
        navigate(path)
    }

    const startReRun = (option: ReRunOptions) => {
        BackendAPICaller.reRunMission(mission.id, option === ReRunOptions.ReRunFailed).then((mission) =>
            navigateToHome()
        )
    }

    return (
        <Centered>
            <Tooltip title={TranslateText('Re-run the mission')} hidden={isOpen}>
                <Button
                    ref={anchorRef}
                    variant="ghost_icon"
                    id="anchor-default"
                    aria-haspopup="true"
                    aria-expanded={isOpen}
                    aria-controls="menu-default"
                    onClick={() => (isOpen ? closeMenu() : openMenu())}
                >
                    <Icon
                        name={Icons.Replay}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={24}
                    />
                </Button>
            </Tooltip>
            <EdsProvider density="compact">
                <Menu
                    open={isOpen}
                    id="menu-default"
                    aria-labelledby="anchor-default"
                    onClose={closeMenu}
                    anchorEl={anchorRef.current}
                >
                    <Menu.Item onClick={() => startReRun(ReRunOptions.ReRun)}>
                        {TranslateText('Re-run full mission')}
                    </Menu.Item>
                    <Menu.Item onClick={() => startReRun(ReRunOptions.ReRunFailed)}>
                        {TranslateText('Re-run failed and cancelled tasks in the mission')}
                    </Menu.Item>
                </Menu>
            </EdsProvider>
        </Centered>
    )
}
