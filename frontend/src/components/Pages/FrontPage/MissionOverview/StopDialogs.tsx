import { Button, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { TranslateText, useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useState, useEffect } from 'react'
import { tokens } from '@equinor/eds-tokens'
import { Mission, MissionStatus } from 'models/Mission'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot, RobotStatus } from 'models/Robot'

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

interface MissionList {
    missions: Mission[]
}

export enum ControlButton {
    Pause,
    Stop,
    Resume,
}

export const StopMissionDialog = ({ mission }: MissionProps): JSX.Element => {
    const [isStopMissionDialogOpen, setIsStopMissionDialogOpen] = useState<boolean>(false)
    const [missionId, setMissionId] = useState<string>()
    const { handleClick } = useMissionControlContext()

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
                            onClick={() => handleClick(ControlButton.Stop, mission)}
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
    
    const openDialog = () => {
        setIsStopRobotDialogOpen(true)
    }

    const closeDialog = () => {
        setIsStopRobotDialogOpen(false)
    }

    const stopAll = () => {
        BackendAPICaller.getEnabledRobots().then((robots: Robot[]) => {
            for (var robot of robots) {
                BackendAPICaller.postSafePosition(robot.id)
            }
        })
        closeDialog()
        return
    }

    const resetRobots = () => {
        BackendAPICaller.getEnabledRobots().then(async (robots: Robot[]) => {
            for (var robot of robots) {
                await BackendAPICaller.resetRobotState(robot.id)
            }
        })
        closeDialog()
        setStatusSafePosition(false)
    }

    const removeMissions = () => {
        BackendAPICaller.getEnabledRobots().then(async (robots: Robot[]) => {
            for (var robot of robots) {
                await BackendAPICaller.removeAllMissions(robot.id)
            }
        })
        
        resetRobots()
    }


    useEffect(() => {
        BackendAPICaller.getEnabledRobots().then((robots: Robot[]) => {
            for (var robot of robots) {
                if (robot.status == RobotStatus.SafePosition){
                    setStatusSafePosition(true)
                }
            }
        })
    }, [[openDialog]])

    return (
        <>  
            {statusSafePosition==false && (
            <><StyledButton>
                    <Button color="danger" variant="outlined" onClick={openDialog}>
                        <Square style={{ background: tokens.colors.interactive.danger__resting.hex }} />
                        {TranslateText('Send robots to safe zone')}
                    </Button>
                </StyledButton><StyledDialog open={isStopRobotDialogOpen} isDismissable>
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
                                    } }
                                >
                                    {TranslateText('Cancel')}
                                </Button>
                                <Button variant="contained" color="danger" onClick={stopAll}>
                                    {TranslateText('Send robots to safe zone')}
                                </Button>
                            </StyledDisplayButtons>
                        </Dialog.Actions>
                    </StyledDialog></>
            )}
            {statusSafePosition==true && (
            <><StyledButton>
                    <Button color="danger" variant="outlined" onClick={openDialog}>
                        <Icon
                            name={Icons.PlayTriangle}
                            size={24}
                        />
                        {TranslateText('Dismiss robots from safe zone')}
                    </Button>
                </StyledButton><StyledDialog open={isStopRobotDialogOpen} isDismissable>
                        <Dialog.Header>
                            <Dialog.Title>
                                <Typography variant="h5">{TranslateText('Dismiss robots from safe zone') + '?'}</Typography>
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
                                    } }
                                >
                                    {TranslateText('Cancel')}
                                </Button>
                                <Button variant="contained" color="danger" onClick={removeMissions}>
                                    {TranslateText('Remove all missions')}
                                </Button>
                                <Button variant="contained" color="danger" onClick={resetRobots}>
                                    {TranslateText('Continue missions')}
                                </Button>
                            </StyledDisplayButtons>
                        </Dialog.Actions>
                    </StyledDialog></>
            )}
        </>
    )
}
