import { Button, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { tokens } from '@equinor/eds-tokens'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { TaskType } from 'models/Task'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { RobotFlotillaStatus } from 'models/Robot'

const StyledDisplayButtons = styled.div`
    display: flex;
    width: calc(100vw * 0.7);
    max-width: 410px;
    flex-direction: columns;
    justify-content: flex-end;
    gap: 0.5rem;
`

const StyledDialog = styled(Dialog)`
    display: grid;
    width: calc(100vw * 0.8);
    max-width: 450px;
`

const StyledText = styled.div`
    display: grid;
    gird-template-rows: auto, auto;
    gap: 1rem;
`

const StyledButton = styled(Button)`
    width: 210px;
    background-color: white;
`

interface MissionProps {
    missionName: string
    robotId: string
    missionTaskType: TaskType
}

export enum MissionStatusRequest {
    Pause,
    Stop,
    Resume,
}

const DialogContent = ({ missionTaskType }: { missionTaskType: TaskType }) => {
    const { TranslateText } = useLanguageContext()
    switch (missionTaskType) {
        case TaskType.Localization:
            return (
                <StyledText>
                    <Typography variant="body_long">
                        {TranslateText('Stop button pressed during localization warning text')}
                    </Typography>
                    <Typography variant="body_long">
                        {TranslateText('Stop button pressed confirmation text')}
                    </Typography>
                </StyledText>
            )
        case TaskType.ReturnHome:
            return (
                <StyledText>
                    <Typography variant="body_long">
                        {TranslateText('Stop button pressed during return home warning text')}
                    </Typography>
                    <Typography variant="body_long">
                        {TranslateText('Stop button pressed confirmation text')}
                    </Typography>
                </StyledText>
            )
        default:
            return (
                <StyledText>
                    <Typography variant="body_long">{TranslateText('Stop button pressed warning text')}</Typography>
                    <Typography variant="body_long">
                        {TranslateText('Stop button pressed confirmation text')}
                    </Typography>
                </StyledText>
            )
    }
}

export const StopMissionDialog = ({ missionName, robotId, missionTaskType }: MissionProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const [isStopMissionDialogOpen, setIsStopMissionDialogOpen] = useState<boolean>(false)
    const { updateRobotMissionState } = useMissionControlContext()

    return (
        <>
            <Button variant="ghost_icon" onClick={() => setIsStopMissionDialogOpen(true)}>
                <Icon
                    name={Icons.StopButton}
                    style={{ color: tokens.colors.interactive.secondary__resting.rgba }}
                    size={40}
                />
            </Button>

            <StyledDialog open={isStopMissionDialogOpen} isDismissable>
                <Dialog.Header>
                    <Dialog.Title>
                        <Typography variant="h5">
                            {TranslateText('Stop mission')} <strong>'{missionName}'</strong>?{' '}
                        </Typography>
                    </Dialog.Title>
                </Dialog.Header>
                <Dialog.CustomContent>
                    <DialogContent missionTaskType={missionTaskType} />
                </Dialog.CustomContent>
                <Dialog.Actions>
                    <StyledDisplayButtons>
                        <Button variant="outlined" color="danger" onClick={() => setIsStopMissionDialogOpen(false)}>
                            {TranslateText('Cancel')}
                        </Button>
                        <Button
                            variant="contained"
                            color="danger"
                            onClick={() => updateRobotMissionState(MissionStatusRequest.Stop, robotId)}
                        >
                            {TranslateText('Stop mission')}
                        </Button>
                    </StyledDisplayButtons>
                </Dialog.Actions>
            </StyledDialog>
        </>
    )
}

export const StopRobotDialog = (): JSX.Element => {
    const [isStopRobotDialogOpen, setIsStopRobotDialogOpen] = useState<boolean>(false)
    const { enabledRobots } = useRobotContext()
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const { setAlert } = useAlertContext()

    const safeZoneActivated =
        enabledRobots.find(
            (r) =>
                r.currentInstallation.installationCode === installationCode &&
                r.flotillaStatus === RobotFlotillaStatus.SafeZone
        ) !== undefined

    const openDialog = async () => {
        setIsStopRobotDialogOpen(true)
    }

    const closeDialog = async () => {
        setIsStopRobotDialogOpen(false)
    }

    const stopAll = () => {
        BackendAPICaller.sendRobotsToSafePosition(installationCode).catch((e) => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent translatedMessage={TranslateText('Failed to send robots to a safe zone')} />,
                AlertCategory.ERROR
            )
        })
        closeDialog()
        return
    }

    const resetRobots = () => {
        BackendAPICaller.clearEmergencyState(installationCode).catch((e) => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent
                    translatedMessage={TranslateText('Failed to release robots from safe zone')}
                />,
                AlertCategory.ERROR
            )
        })
        closeDialog()
    }

    return (
        <>
            <StyledButton color="danger" variant="outlined" onClick={openDialog}>
                {!safeZoneActivated ? (
                    <>{TranslateText('Send robots to safe zone')}</>
                ) : (
                    <>{TranslateText('Dismiss robots from safe zone')}</>
                )}
            </StyledButton>
            <StyledDialog open={isStopRobotDialogOpen} isDismissable>
                <Dialog.Header>
                    <Dialog.Title>
                        <Typography variant="h5">
                            {!safeZoneActivated
                                ? TranslateText('Send robots to safe zone') + '?'
                                : TranslateText('Dismiss robots from safe zone') + '?'}
                        </Typography>
                    </Dialog.Title>
                </Dialog.Header>
                <Dialog.CustomContent>
                    <StyledText>
                        <Typography variant="body_long">
                            {!safeZoneActivated
                                ? TranslateText('Send robots to safe zone long text')
                                : TranslateText('Dismiss robots from safe zone long text')}
                        </Typography>
                        <Typography variant="body_long">
                            {!safeZoneActivated
                                ? TranslateText('Send robots to safe confirmation text')
                                : TranslateText('Dismiss robots from safe confirmation text')}
                        </Typography>
                    </StyledText>
                </Dialog.CustomContent>
                <Dialog.Actions>
                    <StyledDisplayButtons>
                        <Button
                            variant="outlined"
                            color="danger"
                            onClick={() => {
                                setIsStopRobotDialogOpen(false)
                            }}
                        >
                            {TranslateText('Cancel')}
                        </Button>
                        {!safeZoneActivated ? (
                            <Button variant="contained" color="danger" onClick={stopAll}>
                                {TranslateText('Send robots to safe zone')}
                            </Button>
                        ) : (
                            <Button variant="contained" color="danger" onClick={resetRobots}>
                                {TranslateText('Continue missions')}
                            </Button>
                        )}
                    </StyledDisplayButtons>
                </Dialog.Actions>
            </StyledDialog>
        </>
    )
}
