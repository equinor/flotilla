import { Button, EdsProvider, Icon, Menu } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { BackendAPICaller } from 'api/ApiCaller'
import { config } from 'config'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useRef, useState } from 'react'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'
import { Mission } from 'models/Mission'
import { ScheduleMissionWithConfirmDialogs } from '../ConfirmScheduleDialogs/ConfirmScheduleDialog'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

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
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to rerun mission')} />,
                    AlertCategory.ERROR
                )
            )
        setIsLocationVerificationOpen(false)
    }

    return (
        <Centered>
            <Button
                variant="outlined"
                ref={anchorRef}
                id="anchor-default"
                aria-haspopup="true"
                aria-expanded={isOpen}
                aria-controls="menu-default"
                onClick={() => {
                    hasFailedTasks ? setIsOpen(!isOpen) : setSelectedRerunOption(ReRunOptions.ReRun)
                    !hasFailedTasks && setIsLocationVerificationOpen(true)
                }}
            >
                <Icon name={Icons.Replay} size={24} />
                {TranslateText('Rerun mission')}
            </Button>
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
                        {TranslateText('Rerun full mission')}
                    </Menu.Item>
                    {hasFailedTasks && (
                        <Menu.Item
                            onClick={() => {
                                setSelectedRerunOption(ReRunOptions.ReRunFailed)
                                setIsLocationVerificationOpen(true)
                            }}
                        >
                            {TranslateText('Rerun failed and cancelled tasks in the mission')}
                        </Menu.Item>
                    )}
                </Menu>
            </EdsProvider>
            {isLocationVerificationOpen && (
                <ScheduleMissionWithConfirmDialogs
                    scheduleMissions={() => startReRun(selectedRerunOption!)}
                    closeDialog={() => setIsLocationVerificationOpen(false)}
                    robotId={mission.robot.id}
                    missionDeckNames={[mission.area?.deckName ?? '']}
                />
            )}
        </Centered>
    )
}
