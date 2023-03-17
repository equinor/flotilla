import { Mission, MissionStatus } from 'models/Mission'
import { CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { useEffect, useState } from 'react'
import { useApi } from 'api/ApiCaller'
import { tokens } from '@equinor/eds-tokens'
import { useErrorHandler } from 'react-error-boundary'
import { Task, TaskStatus } from 'models/Task'

interface MissionProps {
    mission: Mission
}

export enum ControlButton {
    Pause,
    Stop,
    Resume,
}

const checkIfTasksStarted = (tasks: Task[]): boolean => {
    return tasks.some((task) => task.status !== TaskStatus.NotStarted)
}

export function MissionControlButtons({ mission }: MissionProps) {
    const [isWaitingForResponse, setIsWaitingForResponse] = useState<boolean>(false)
    const apiCaller = useApi()
    const handleError = useErrorHandler()
    const handleClick = (button: ControlButton) => {
        switch (button) {
            case ControlButton.Pause: {
                setIsWaitingForResponse(true)
                apiCaller.pauseMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                //.catch((e) => handleError(e))
                break
            }
            case ControlButton.Resume: {
                setIsWaitingForResponse(true)
                apiCaller.resumeMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                //.catch((e) => handleError(e))
                break
            }
            case ControlButton.Stop: {
                setIsWaitingForResponse(true)
                apiCaller.stopMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
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
                <>
                    <Icon
                        name={Icons.StopButton}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                    <Icon
                        name={Icons.PauseButton}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Pause)}
                    />
                </>
            )
        } else if (missionStatus === MissionStatus.Paused) {
            return (
                <>
                    <Icon
                        name={Icons.StopButton}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                    <Icon
                        name={Icons.PlayButton}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Resume)}
                    />
                </>
            )
        }
        return <></>
    }
    return checkIfTasksStarted(mission.tasks) ? <>{renderControlIcon(mission.status)}</> : <></>
}
