import { Icon, Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Mission, MissionStatus } from 'models/Mission'
import styled from 'styled-components'
import { MissionProgressDisplay } from './MissionTagDisplay'
import { MissionStatusDisplay } from './MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { pause_circle, stop, play_circle, pause } from '@equinor/eds-icons'
import { useState } from 'react'
import { useApi } from 'api/ApiCaller'

Icon.add({ pause_circle, play_circle, stop })
interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 300px;
    padding: 10px;
`
const HorisontalContent = styled.div`
    display: grid;
    grid-template-columns: auto auto 40px 40px;
    align-items: end;
`

export function OngoingMissionCard({ mission }: MissionProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        let path = '/mission/' + mission.id
        navigate(path)
    }
    return (
        <StyledMissionCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <Typography variant="h6" onClick={routeChange}>
                INSPECTION
            </Typography>
            <Typography>{mission.name}</Typography>
            <HorisontalContent>
                <MissionStatusDisplay status={mission.missionStatus} />
                <MissionProgressDisplay tasks={mission.tasks} />
                <MissionControlButtons mission={mission} />
            </HorisontalContent>
        </StyledMissionCard>
    )
}

function MissionControlButtons({ mission }: MissionProps) {
    const [status, setStatus] = useState(mission.missionStatus)
    const apiCaller = useApi()
    enum ControlButton {
        Pause,
        Stop,
        Resume,
    }

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
        if (status == MissionStatus.Paused) {
            return (
                <>
                    <Icon
                        name="play_circle"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Resume)}
                    />
                    <Icon
                        name="stop"
                        style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                        size={32}
                        onClick={() => handleClick(ControlButton.Stop)}
                    />
                </>
            )
        }
        return (
            <Icon
                name="pause_circle"
                style={{ color: tokens.colors.interactive.primary__resting.rgba }}
                size={32}
                onClick={() => handleClick(ControlButton.Pause)}
            />
        )
    }

    return <>{renderControlIcon(status)}</>
}
