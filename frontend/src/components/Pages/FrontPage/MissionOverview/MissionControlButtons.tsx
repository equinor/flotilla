import { Mission, MissionStatus } from 'models/Mission'
import { Icon } from '@equinor/eds-core-react'
import { useEffect, useState } from 'react'
import { useApi } from 'api/ApiCaller'
import { pause_circle, stop, play_circle } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'

Icon.add({ pause_circle, play_circle, stop })

interface MissionProps {
    mission: Mission
}

export function MissionControlButtons({ mission }: MissionProps) {
    const [status, setStatus] = useState(mission.missionStatus)
    const apiCaller = useApi()
    enum ControlButton {
        Pause,
        Stop,
        Resume,
    }

    useEffect(() => {
        setStatus(mission.missionStatus)
    }, [mission.missionStatus])

    const handleClick = (button: ControlButton) => {
        switch (button) {
            case ControlButton.Pause: {
                apiCaller.pauseMission(mission.robot.id)
                setStatus(MissionStatus.Paused)
                break
            }
            case ControlButton.Resume: {
                apiCaller.resumeMission(mission.robot.id)
                setStatus(MissionStatus.Ongoing)
                break
            }
            case ControlButton.Stop: {
                apiCaller.stopMission(mission.robot.id)
                setStatus(MissionStatus.Cancelled)
                break
            }
        }
    }

    const renderControlIcon = (status: MissionStatus) => {
        if (status === MissionStatus.Ongoing) {
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
        }
        else if (status === MissionStatus.Paused) {
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
        }
        return (
            <></>
        )
    }

    return <>{renderControlIcon(status)}</>
}
