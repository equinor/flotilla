import { Button, EdsProvider, Icon, Menu } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { BackendAPICaller } from 'api/ApiCaller'
import { config } from 'config'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useRef, useState } from 'react'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { Mission } from 'models/Mission'
import { ScheduleMissionWithConfirmDialogs } from '../ConfirmScheduleDialogs/ConfirmScheduleDialog'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

const Centered = styled.div`
    display: flex;
    align-content: center;
    align-items: center;
    justify-content: center;
`
const StyledButton = styled(Button)`
    background-color: none;
`

interface MissionProps {
    mission: Mission
    hasFailedTasks: boolean
    smallButton: boolean
}

enum ReRunOptions {
    ReRun,
    ReRunFailed,
}

export const MissionRestartButton = ({ mission, hasFailedTasks, smallButton }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
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
            .catch(() => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to rerun mission')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent translatedMessage={TranslateText('Failed to rerun mission')} />,
                    AlertCategory.ERROR
                )
            })
        setIsLocationVerificationOpen(false)
    }

    return (
        <Centered>
            <StyledButton
                variant={smallButton ? 'ghost_icon' : 'outlined'}
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
                <Icon name={smallButton ? Icons.AddOutlined : Icons.Add} size={24} />
                {!smallButton && TranslateText('Queue mission')}
            </StyledButton>
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
                    missionInspectionAreaNames={[mission.inspectionArea?.inspectionAreaName ?? '']}
                />
            )}
        </Centered>
    )
}
