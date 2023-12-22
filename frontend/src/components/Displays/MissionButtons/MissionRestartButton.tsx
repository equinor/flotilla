import { Button, EdsProvider, Icon, Menu, Tooltip } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { BackendAPICaller } from 'api/ApiCaller'
import { config } from 'config'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useRef, useState } from 'react'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'

const Centered = styled.div`
    display: flex;
    align-content: center;
    align-items: center;
`

interface MissionProps {
    missionId: string
    hasFailedTasks: boolean
}

export enum ReRunOptions {
    ReRun,
    ReRunFailed,
}

export const MissionRestartButton = ({ missionId, hasFailedTasks }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert } = useAlertContext()
    const [isOpen, setIsOpen] = useState<boolean>(false)
    const anchorRef = useRef<HTMLButtonElement>(null)

    let navigate = useNavigate()
    const navigateToHome = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/FrontPage`
        navigate(path)
    }

    const startReRun = (option: ReRunOptions) =>
        BackendAPICaller.reRunMission(missionId, option === ReRunOptions.ReRunFailed)
            .then(() => navigateToHome())
            .catch(() =>
                setAlert(AlertType.RequestFail, <FailedRequestAlertContent message={'Failed to rerun missions'} />)
            )

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
                    onClick={() => setIsOpen(!isOpen)}
                >
                    <Icon
                        name={Icons.Replay}
                        style={{ color: tokens.colors.interactive.primary__resting.hex }}
                        size={24}
                    />
                </Button>
            </Tooltip>
            <EdsProvider density="compact">
                <Menu
                    open={isOpen}
                    id="menu-default"
                    aria-labelledby="anchor-default"
                    onClose={() => setIsOpen(false)}
                    anchorEl={anchorRef.current}
                >
                    <Menu.Item onClick={() => startReRun(ReRunOptions.ReRun)}>
                        {TranslateText('Re-run full mission')}
                    </Menu.Item>
                    {hasFailedTasks && (
                        <Menu.Item onClick={() => startReRun(ReRunOptions.ReRunFailed)}>
                            {TranslateText('Re-run failed and cancelled tasks in the mission')}
                        </Menu.Item>
                    )}
                </Menu>
            </EdsProvider>
        </Centered>
    )
}
