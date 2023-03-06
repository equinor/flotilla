import { Mission, MissionStatus } from 'models/Mission'
import { Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { useApi } from 'api/ApiCaller'
import { tokens } from '@equinor/eds-tokens'
import { ControlMissionResponse } from 'models/ControlMissionResponse'
import { IsarTask, IsarTaskStatus } from 'models/IsarTask'

interface MissionProps {
    mission: Mission
}

export enum ControlButton {
    Pause,
    Stop,
    Resume,
}

enum IsarMissionResponse {
    Paused = 'paused',
    Stopped = 'cancelled',
    Ongoing = 'in_progress',
    Unknown = 'unknown',
}

export type MissionResponse = IsarMissionResponse | MissionStatus

const mapMissionStatusToIsarStatus = (status: MissionStatus): IsarMissionResponse => {
    if (status === MissionStatus.Ongoing) return IsarMissionResponse.Ongoing
    if (status === MissionStatus.Paused) return IsarMissionResponse.Paused
    if (status === MissionStatus.Aborted) return IsarMissionResponse.Stopped
    return IsarMissionResponse.Unknown
}

const checkIfTasksStarted = (tasks: IsarTask[]): boolean => {
    var isStarted = false
    tasks.map((task: IsarTask) => {
        if (task.taskStatus !== IsarTaskStatus.NotStarted) {
            isStarted = true
        }
    })
    return isStarted
}

export function MissionControlButtons({ mission }: MissionProps) {
    const [isarResponse, setIsarResponse] = useState<MissionResponse>(
        mapMissionStatusToIsarStatus(mission.missionStatus) as IsarMissionResponse
    )
    const apiCaller = useApi()
    const handleClick = (button: ControlButton) => {
        switch (button) {
            case ControlButton.Pause: {
                setIsarResponse(IsarMissionResponse.Unknown)
                apiCaller.pauseMission(mission.robot.id).then((response: ControlMissionResponse) => {
                    setIsarResponse(response.missionStatus)
                })
                break
            }
            case ControlButton.Resume: {
                setIsarResponse(IsarMissionResponse.Unknown)
                apiCaller.resumeMission(mission.robot.id).then((response: ControlMissionResponse) => {
                    setIsarResponse(response.missionStatus)
                })
                break
            }
            case ControlButton.Stop: {
                setIsarResponse(IsarMissionResponse.Unknown)
                apiCaller.stopMission(mission.robot.id).then((response: ControlMissionResponse) => {
                    setIsarResponse(response.missionStatus)
                })
                break
            }
        }
    }

    const renderControlIcon = (missionStatus: MissionResponse) => {
        if (missionStatus === IsarMissionResponse.Ongoing) {
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
        } else if (missionStatus === IsarMissionResponse.Paused) {
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
        } else if (missionStatus === IsarMissionResponse.Unknown) {
            return <Icon name={Icons.Wait} size={32} />
        }
        return <></>
    }
    return checkIfTasksStarted(mission.tasks) ? <>{renderControlIcon(isarResponse)}</> : <></>
}
