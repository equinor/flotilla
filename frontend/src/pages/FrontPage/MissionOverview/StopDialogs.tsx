import { Button, Dialog, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useContext, useState } from 'react'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { RobotStatus } from 'models/Robot'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'

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
const StyledDockButton = styled(Button)`
    height: auto;
    min-height: ${tokens.shape.button.minHeight};
    white-space: nowrap;
`
const ContainButton = styled.div`
    display: block;
`

interface MissionProps {
    missionName?: string
    robotId: string
}

export enum MissionStatusRequest {
    Pause,
    Stop,
    Resume,
}

const DialogContent = () => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledText>
            <Typography variant="body_long">{TranslateText('Skip button pressed warning text')}</Typography>
            <Typography variant="body_long">{TranslateText('Skip button pressed confirmation text')}</Typography>
        </StyledText>
    )
}

export const SkipMissionDialog = ({
    missionName,
    robotId,
    isSkipMissionDialogOpen,
    toggleDialog,
}: MissionProps & { isSkipMissionDialogOpen: boolean; toggleDialog: () => void }) => {
    const { TranslateText } = useLanguageContext()
    const { updateRobotMissionState } = useMissionControlContext()

    return (
        <StyledDialog open={isSkipMissionDialogOpen} isDismissable>
            <Dialog.Header>
                <Dialog.Title>
                    <Typography variant="h5">
                        {missionName ? TranslateText('Skip mission:') : TranslateText('No mission running')}{' '}
                        {missionName}
                    </Typography>
                </Dialog.Title>
            </Dialog.Header>
            <Dialog.CustomContent>
                <DialogContent />
            </Dialog.CustomContent>
            <Dialog.Actions>
                <StyledDisplayButtons>
                    <Button variant="outlined" color="danger" onClick={toggleDialog}>
                        {TranslateText('Cancel')}
                    </Button>
                    <Button
                        variant="contained"
                        color="danger"
                        onClick={() => updateRobotMissionState(MissionStatusRequest.Stop, robotId)}
                    >
                        {TranslateText('Skip mission')}
                    </Button>
                </StyledDisplayButtons>
            </Dialog.Actions>
        </StyledDialog>
    )
}

export const StopRobotDialog = () => {
    const [isStopRobotDialogOpen, setIsStopRobotDialogOpen] = useState<boolean>(false)
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const { TranslateText } = useLanguageContext()
    const { raiseAlert } = useAlertContext()
    const backendApi = useBackendApi()

    const dockActivated =
        enabledRobots.find((r) => r.status === RobotStatus.Lockdown || r.status === RobotStatus.GoingToLockdown) !==
        undefined

    const openDialog = async () => {
        setIsStopRobotDialogOpen(true)
    }

    const closeDialog = async () => {
        setIsStopRobotDialogOpen(false)
    }

    const stopAll = () => {
        backendApi.sendRobotsToDockingPosition(installation.installationCode).catch(() => {
            raiseAlert(AlertType.RequestFail, {
                kind: 'requestFail',
                message: TranslateText('Failed to send robots to a dock'),
            })
        })
        closeDialog()
        return
    }

    const resetRobots = () => {
        backendApi.clearEmergencyState(installation.installationCode).catch(() => {
            raiseAlert(AlertType.RequestFail, {
                kind: 'requestFail',
                message: TranslateText('Failed to release robots from dock'),
            })
        })
        closeDialog()
    }

    return (
        <>
            <ContainButton>
                <StyledDockButton color="danger" variant="contained" onClick={openDialog}>
                    {!dockActivated ? (
                        <>{TranslateText('Send robots to dock')}</>
                    ) : (
                        <>{TranslateText('Dismiss robots from dock')}</>
                    )}
                </StyledDockButton>
            </ContainButton>
            <StyledDialog open={isStopRobotDialogOpen} isDismissable>
                <Dialog.Header>
                    <Dialog.Title>
                        <Typography variant="h5">
                            {!dockActivated
                                ? TranslateText('Send robots to dock') + '?'
                                : TranslateText('Dismiss robots from dock') + '?'}
                        </Typography>
                    </Dialog.Title>
                </Dialog.Header>
                <Dialog.CustomContent>
                    <StyledText>
                        <Typography variant="body_long">
                            {!dockActivated
                                ? TranslateText('Send robots to dock long text')
                                : TranslateText('Dismiss robots from dock long text')}
                        </Typography>
                        <Typography variant="body_long">
                            {!dockActivated
                                ? TranslateText('Send robots to dock confirmation text')
                                : TranslateText('Dismiss robots from dock confirmation text')}
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
                        {!dockActivated ? (
                            <Button variant="contained" color="danger" onClick={stopAll}>
                                {TranslateText('Send robots to dock')}
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
