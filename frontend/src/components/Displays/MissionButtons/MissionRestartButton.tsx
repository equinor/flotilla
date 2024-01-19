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
import { ScheduleMissionWithLocalizationVerificationDialog } from 'components/Displays/LocalizationVerification/ScheduleMissionWithLocalizationVerification'
import { Mission } from 'models/Mission'

const Centered = styled.div`
    display: flex;
    align-content: center;
    align-items: center;
`

interface MissionProps {
    mission: Mission
    hasFailedTasks: boolean
}

enum ReRunOptions {
    ReRun,
    ReRunFailed,
}

export const MissionRestartButton = ({ mission, hasFailedTasks }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert } = useAlertContext()
    const [isOpen, setIsOpen] = useState<boolean>(false)
    const [isLocationVerificationOpen, setIsLocationVerificationOpen] = useState<boolean>(false)
    const [selectedRerunOption, setSelectedRerunOption] = useState<ReRunOptions>()
    const anchorRef = useRef<HTMLButtonElement>(null)

    let navigate = useNavigate()
    const navigateToHome = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/FrontPage`
        navigate(path)
    }

    const startReRun = (option: ReRunOptions) => {
        BackendAPICaller.reRunMission(mission.id, option === ReRunOptions.ReRunFailed)
            .then(() => navigateToHome())
            .catch(() =>
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to rerun mission')} />
                )
            )
        setIsLocationVerificationOpen(false)
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
                    <Menu.Item
                        onClick={() => {
                            setSelectedRerunOption(ReRunOptions.ReRun)
                            setIsLocationVerificationOpen(true)
                        }}
                    >
                        {TranslateText('Re-run full mission')}
                    </Menu.Item>
                    {hasFailedTasks && (
                        <Menu.Item
                            onClick={() => {
                                setSelectedRerunOption(ReRunOptions.ReRunFailed)
                                setIsLocationVerificationOpen(true)
                            }}
                        >
                            {TranslateText('Re-run failed and cancelled tasks in the mission')}
                        </Menu.Item>
                    )}
                </Menu>
            </EdsProvider>
            {isLocationVerificationOpen && (
                <ScheduleMissionWithLocalizationVerificationDialog
                    scheduleMissions={() => startReRun(selectedRerunOption!)}
                    closeDialog={() => setIsLocationVerificationOpen(false)}
                    robotId={mission.robot.id}
                    missionDeckNames={[mission.area?.deckName ?? '']}
                />
            )}
        </Centered>
    )
}
