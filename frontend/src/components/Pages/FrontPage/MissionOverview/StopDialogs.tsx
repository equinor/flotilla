import { Button, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { tokens } from '@equinor/eds-tokens'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useSafeZoneContext } from 'components/Contexts/SafeZoneContext'
import { TaskType } from 'models/Task'

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

const StyledButton = styled.div`
    width: 250px;
`

const Square = styled.div`
    width: 12px;
    height: 12px;
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
    const { safeZoneStatus } = useSafeZoneContext()
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()

    const openDialog = async () => {
        setIsStopRobotDialogOpen(true)
    }

    const closeDialog = async () => {
        setIsStopRobotDialogOpen(false)
    }

    const stopAll = () => {
        BackendAPICaller.sendRobotsToSafePosition(installationCode)
        closeDialog()
        return
    }

    const resetRobots = () => {
        BackendAPICaller.clearEmergencyState(installationCode)
        closeDialog()
    }

    return (
        <>
            <StyledButton>
                <Button color="danger" variant="outlined" onClick={openDialog}>
                    {!safeZoneStatus ? (
                        <>
                            <Square style={{ background: tokens.colors.interactive.danger__resting.hex }} />
                            {TranslateText('Send robots home')}
                        </>
                    ) : (
                        <>
                            <Icon name={Icons.PlayTriangle} size={24} />
                            {TranslateText('Dismiss robots from safe zone')}
                        </>
                    )}
                </Button>
            </StyledButton>
            <StyledDialog open={isStopRobotDialogOpen} isDismissable>
                <Dialog.Header>
                    <Dialog.Title>
                        <Typography variant="h5">
                            {!safeZoneStatus
                                ? TranslateText('Send robots home') + '?'
                                : TranslateText('Dismiss robots from home') + '?'}
                        </Typography>
                    </Dialog.Title>
                </Dialog.Header>
                <Dialog.CustomContent>
                    <StyledText>
                        <Typography variant="body_long">
                            {!safeZoneStatus
                                ? TranslateText('Send robots home long text')
                                : TranslateText('Dismiss robots from home long text')}
                        </Typography>
                        <Typography variant="body_long">
                            {!safeZoneStatus
                                ? TranslateText('Send robots home confirmation text')
                                : TranslateText('Dismiss robots from home confirmation text')}
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
                        {!safeZoneStatus ? (
                            <Button variant="contained" color="danger" onClick={stopAll}>
                                {TranslateText('Send robots home')}
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
