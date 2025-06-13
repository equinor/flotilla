import { Button, Dialog, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState } from 'react'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { RobotFlotillaStatus } from 'models/Robot'
import { StyledButton } from 'components/Styles/StyledComponents'

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
const StyledDockButton = styled(StyledButton)`
    background-color: white;
`
const ContainButton = styled.div`
    display: block;
`

interface MissionProps {
    missionName?: string
    robotId: string
    isReturnToHomeMission: boolean
}

export enum MissionStatusRequest {
    Pause,
    Stop,
    Resume,
}

const DialogContent = ({ isReturnToHomeMission }: { isReturnToHomeMission: boolean }) => {
    const { TranslateText } = useLanguageContext()

    if (isReturnToHomeMission) {
        return (
            <StyledText>
                <Typography variant="body_long">
                    {TranslateText('Skip button pressed during return home warning text')}
                </Typography>
                <Typography variant="body_long">{TranslateText('Skip button pressed confirmation text')}</Typography>
            </StyledText>
        )
    }
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
    isReturnToHomeMission,
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
                <DialogContent isReturnToHomeMission={isReturnToHomeMission} />
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
    const { enabledRobots } = useRobotContext()
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const { setAlert, setListAlert } = useAlertContext()

    const dockActivated = enabledRobots.find((r) => r.flotillaStatus === RobotFlotillaStatus.Home) !== undefined

    const openDialog = async () => {
        setIsStopRobotDialogOpen(true)
    }

    const closeDialog = async () => {
        setIsStopRobotDialogOpen(false)
    }

    const stopAll = () => {
        BackendAPICaller.sendRobotsToDockingPosition(installationCode).catch(() => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent translatedMessage={TranslateText('Failed to send robots to a dock')} />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent translatedMessage={TranslateText('Failed to send robots to a dock')} />,
                AlertCategory.ERROR
            )
        })
        closeDialog()
        return
    }

    const resetRobots = () => {
        BackendAPICaller.clearEmergencyState(installationCode).catch(() => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent translatedMessage={TranslateText('Failed to release robots from dock')} />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Failed to release robots from dock')}
                />,
                AlertCategory.ERROR
            )
        })
        closeDialog()
    }

    return (
        <>
            <ContainButton>
                <StyledDockButton color="danger" variant="outlined" onClick={openDialog}>
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
