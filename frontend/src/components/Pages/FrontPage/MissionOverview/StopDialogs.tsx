import { Button, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useState, useEffect } from 'react'
import { tokens } from '@equinor/eds-tokens'
import { Mission, MissionStatus } from 'models/Mission'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot, RobotStatus } from 'models/Robot'
import { useInstallationContext } from 'components/Contexts/InstallationContext'

const StyledDisplayButtons = styled.div`
    display: flex;
    width: 410px;
    flex-direction: columns;
    justify-content: flex-end;
    gap: 0.5rem;
`

const StyledDialog = styled(Dialog)`
    display: grid;
    width: 450px;
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
    mission: Mission
}

export enum MissionStatusRequest {
    Pause,
    Stop,
    Resume,
}

export const StopMissionDialog = ({ mission }: MissionProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const [isStopMissionDialogOpen, setIsStopMissionDialogOpen] = useState<boolean>(false)
    const [missionId, setMissionId] = useState<string>()
    const { updateMissionState } = useMissionControlContext()

    const openDialog = () => {
        setIsStopMissionDialogOpen(true)
        setMissionId(mission.id)
    }

    useEffect(() => {
        if (missionId !== mission.id) setIsStopMissionDialogOpen(false)
    }, [mission.id])

    return (
        <>
            <Button variant="ghost_icon" onClick={openDialog}>
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
                            {TranslateText('Stop mission')} <strong>'{mission.name}'</strong>?{' '}
                        </Typography>
                    </Dialog.Title>
                </Dialog.Header>
                <Dialog.CustomContent>
                    <StyledText>
                        <Typography variant="body_long">{TranslateText('Stop button pressed warning text')}</Typography>
                        <Typography variant="body_long">
                            {TranslateText('Stop button pressed confirmation text')}
                        </Typography>
                    </StyledText>
                </Dialog.CustomContent>
                <Dialog.Actions>
                    <StyledDisplayButtons>
                        <Button
                            variant="outlined"
                            color="danger"
                            onClick={() => {
                                setIsStopMissionDialogOpen(false)
                            }}
                        >
                            {TranslateText('Cancel')}
                        </Button>
                        <Button
                            variant="contained"
                            color="danger"
                            onClick={() => updateMissionState(MissionStatusRequest.Stop, mission)}
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
    const [statusSafePosition, setStatusSafePosition] = useState<boolean>(false)
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()

    const openDialog = async () => {
        setIsStopRobotDialogOpen(true)
    }

    const closeDialog = async () => {
        setIsStopRobotDialogOpen(false)
    }

    const stopAll = () => {
        BackendAPICaller.postSafePosition(installationCode)
        closeDialog()
        setStatusSafePosition(true)
        return
    }

    const resetRobots = () => {
        BackendAPICaller.getEnabledRobots()
            .then((robots) =>
                robots.filter((robots) => robots.currentInstallation.toLowerCase() == installationCode.toLowerCase())
            )
            .then(async (robots: Robot[]) => {
                console.log(robots)
                for (var robot of robots) {
                    console.log(robot.name)

                    try {
                        await BackendAPICaller.resetRobotState(robot.id)
                    } catch (e) {
                        console.error(`Failed to POST clear emergency state for ${robot.name}: ` + e)
                    }
                }
            })
        closeDialog()
        setStatusSafePosition(false)
    }

    return (
        <>
            {!statusSafePosition && (
                <>
                    <StyledButton>
                        <Button color="danger" variant="outlined" onClick={openDialog}>
                            <Square style={{ background: tokens.colors.interactive.danger__resting.hex }} />
                            {TranslateText('Send robots to safe zone')}
                        </Button>
                    </StyledButton>
                    <StyledDialog open={isStopRobotDialogOpen} isDismissable>
                        <Dialog.Header>
                            <Dialog.Title>
                                <Typography variant="h5">{TranslateText('Send robots to safe zone') + '?'}</Typography>
                            </Dialog.Title>
                        </Dialog.Header>
                        <Dialog.CustomContent>
                            <StyledText>
                                <Typography variant="body_long">
                                    {TranslateText('Send robots to safe zone long text')}
                                </Typography>
                                <Typography variant="body_long">
                                    {TranslateText('Send robots to safe confirmation text')}
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
                                <Button variant="contained" color="danger" onClick={stopAll}>
                                    {TranslateText('Send robots to safe zone')}
                                </Button>
                            </StyledDisplayButtons>
                        </Dialog.Actions>
                    </StyledDialog>
                </>
            )}
            {statusSafePosition == true && (
                <>
                    <StyledButton>
                        <Button color="danger" variant="outlined" onClick={openDialog}>
                            <Icon name={Icons.PlayTriangle} size={24} />
                            {TranslateText('Dismiss robots from safe zone')}
                        </Button>
                    </StyledButton>
                    <StyledDialog open={isStopRobotDialogOpen} isDismissable>
                        <Dialog.Header>
                            <Dialog.Title>
                                <Typography variant="h5">
                                    {TranslateText('Dismiss robots from safe zone') + '?'}
                                </Typography>
                            </Dialog.Title>
                        </Dialog.Header>
                        <Dialog.CustomContent>
                            <StyledText>
                                <Typography variant="body_long">
                                    {TranslateText('Dismiss robots from safe zone long text')}
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
                                <Button variant="contained" color="danger" onClick={resetRobots}>
                                    {TranslateText('Continue missions')}
                                </Button>
                            </StyledDisplayButtons>
                        </Dialog.Actions>
                    </StyledDialog>
                </>
            )}
        </>
    )
}
