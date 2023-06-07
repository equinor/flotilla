import { Mission, MissionStatus } from 'models/Mission'
import { Button, CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { tokens } from '@equinor/eds-tokens'
import { Task, TaskStatus } from 'models/Task'
import { BackendAPICaller } from 'api/ApiCaller'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { TranslateText } from 'components/Contexts/LanguageContext'

interface MissionProps {
    mission: Mission
}

export enum ControlButton {
    Pause,
    Stop,
    Resume,
}

const ButtonStyle = styled.div`
    display: grid;
    grid-template-columns: 45px 45px;
    align-items: end;
`

const ButtonText = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
`

const checkIfTasksStarted = (tasks: Task[]): boolean => {
    return tasks.some((task) => task.status !== TaskStatus.NotStarted)
}

export function MissionControlButtons({ mission }: MissionProps) {
    const [isWaitingForResponse, setIsWaitingForResponse] = useState<boolean>(false)
    const handleClick = (button: ControlButton) => {
        switch (button) {
            case ControlButton.Pause: {
                setIsWaitingForResponse(true)
                BackendAPICaller.pauseMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                //.catch((e) => handleError(e))
                break
            }
            case ControlButton.Resume: {
                setIsWaitingForResponse(true)
                BackendAPICaller.resumeMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                //.catch((e) => handleError(e))
                break
            }
            case ControlButton.Stop: {
                setIsWaitingForResponse(true)
                BackendAPICaller.stopMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                //.catch((e) => handleError(e))
                break
            }
        }
    }

    const renderControlIcon = (missionStatus: MissionStatus) => {
        if (isWaitingForResponse) {
            return <CircularProgress size={32} />
        } else if (missionStatus === MissionStatus.Ongoing) {
            return (
                <ButtonStyle>
                    <ButtonText>
                        <Button variant="ghost_icon" onClick={() => handleClick(ControlButton.Stop)}>
                            <Icon
                                name={Icons.StopButton}
                                style={{ color: tokens.colors.interactive.secondary__resting.rgba }}
                                size={40}
                            />
                        </Button>
                        <Typography>{TranslateText('Stop')}</Typography>
                    </ButtonText>
                    <ButtonText>
                        <Button variant="ghost_icon" onClick={() => handleClick(ControlButton.Pause)}>
                            <Icon
                                name={Icons.PauseButton}
                                style={{ color: tokens.colors.interactive.secondary__resting.rgba }}
                                size={40}
                            />
                        </Button>
                        <Typography>{TranslateText('Pause')}</Typography>
                    </ButtonText>
                </ButtonStyle>
            )
        } else if (missionStatus === MissionStatus.Paused) {
            return (
                <ButtonStyle>
                    <ButtonText>
                        <Button variant="ghost_icon" onClick={() => handleClick(ControlButton.Stop)}>
                            <Icon
                                name={Icons.StopButton}
                                style={{ color: tokens.colors.interactive.secondary__resting.rgba }}
                                size={40}
                            />
                        </Button>
                        <Typography variant="caption">{TranslateText('Stop')}</Typography>
                    </ButtonText>
                    <ButtonText>
                        <Button variant="ghost_icon" onClick={() => handleClick(ControlButton.Resume)}>
                            <Icon
                                name={Icons.PlayButton}
                                style={{ color: tokens.colors.interactive.secondary__resting.rgba }}
                                size={40}
                            />
                        </Button>
                        <Typography variant="caption">{TranslateText('Start')}</Typography>
                    </ButtonText>
                </ButtonStyle>
            )
        }
        return <></>
    }
    return checkIfTasksStarted(mission.tasks) ? <>{renderControlIcon(mission.status)}</> : <></>
}
