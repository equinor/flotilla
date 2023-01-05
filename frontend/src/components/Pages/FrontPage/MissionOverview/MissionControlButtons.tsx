import { Mission, MissionStatus } from 'models/Mission'
import { Icon } from '@equinor/eds-core-react'
import { useEffect, useState } from 'react'
import { useApi } from 'api/ApiCaller'
import { pause_circle, stop, play_circle, hourglass_empty } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { ControlMissionResponse } from 'models/ControlMissionResponse'

Icon.add({ pause_circle, play_circle, stop, hourglass_empty })

interface MissionProps {
    mission: Mission
}

export enum ControlButton {
    Pause = 'paused',
    Stop = 'stopped',
    Resume = 'in_progress',
    Loading = 'loading',
}

enum IsarMissionResponse {
    Unknown = 'loading',
}

export type MissionResponse = IsarMissionResponse | MissionStatus | ControlButton

const mapMissionStatusToIsarStatus = (status: MissionStatus): string => {
    if (status === MissionStatus.Ongoing) return 'in_progress'
    if (status === MissionStatus.Paused) return 'paused'
    if (status === MissionStatus.Aborted) return 'stopped'
    return 'loading'
}

export function MissionControlButtons({ mission }: MissionProps) {
    const [isarResponse, setIsarResponse] = useState<MissionResponse>(
        mapMissionStatusToIsarStatus(mission.missionStatus) as ControlButton
    )
    const [status, setStatus] = useState<MissionResponse>(mission.missionStatus)
    const apiCaller = useApi()

    const handleClick = (button: ControlButton) => {
        switch (button) {
            case ControlButton.Pause: {
                setIsarResponse(IsarMissionResponse.Unknown)
                apiCaller.pauseMission(mission.robot.id).then((response: ControlMissionResponse) => {
                    setIsarResponse(response.mission_status)
                })
                break
            }
            case ControlButton.Resume: {
                setIsarResponse(IsarMissionResponse.Unknown)
                apiCaller.resumeMission(mission.robot.id).then((response: ControlMissionResponse) => {
                    setIsarResponse(response.mission_status)
                })
                break
            }
            case ControlButton.Stop: {
                apiCaller.stopMission(mission.robot.id)
                break
            }
        }
    }

    const renderControlIcon = (missionStatus: MissionResponse) => {
        if (missionStatus === 'in_progress') {
            return (
                <>
                    <Icon
                        name="stop"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                    <Icon
                        name="pause_circle"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Pause)}
                    />
                </>
            )
        } else if (missionStatus === 'paused') {
            return (
                <>
                    <Icon
                        name="stop"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                    <Icon
                        name="play_circle"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Resume)}
                    />
                </>
            )
        } else if (missionStatus === 'loading') {
            return (
                <>
                    <Icon
                        name="stop"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                    <Icon
                        name="hourglass_empty"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                    />
                </>
            )
        }
        return <></>
    }

    return <>{renderControlIcon(isarResponse)}</>
}
