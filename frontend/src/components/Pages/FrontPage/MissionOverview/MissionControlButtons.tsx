import { Mission, MissionStatus } from 'models/Mission'
import { Icon } from '@equinor/eds-core-react'
import { useState } from 'react'
import { useApi } from 'api/ApiCaller'
import { pause_circle, stop_circle, play_circle, do_not_disturb } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { ControlMissionResponse } from 'models/ControlMissionResponse'
import { IsarTask, IsarTaskStatus } from 'models/IsarTask'

Icon.add({ pause_circle, play_circle, stop_circle, do_not_disturb })

interface MissionProps {
    mission: Mission
}

export enum ControlButton {
    Pause,
    Stop,
    Resume,
}

enum IconsEnum {
    Stop = 'stop_circle',
    Pause = 'pause_circle',
    Play = 'play_circle',
    Wait = 'do_not_disturb',
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
                        name={IconsEnum.Stop}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                    <Icon
                        name={IconsEnum.Pause}
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
                        name={IconsEnum.Stop}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                    <Icon
                        name={IconsEnum.Play}
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Resume)}
                    />
                </>
            )
        } else if (missionStatus === IsarMissionResponse.Unknown) {
            return <Icon name={IconsEnum.Wait} size={32} />
        }
        return <></>
    }
    return checkIfTasksStarted(mission.tasks) ? <>{renderControlIcon(isarResponse)}</> : <></>
}
