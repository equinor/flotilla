import { Mission, MissionStatus } from 'models/Mission'
import { Button, CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { Task, TaskStatus } from 'models/Task'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { StopMissionDialog, ControlButton } from './StopMissionDialog'

interface MissionProps {
    mission: Mission
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
    const { missionControlState, handleClick } = useMissionControlContext()

    const renderControlIcon = (missionStatus: MissionStatus) => {
        if (missionControlState.isWaitingForResponse) {
            return <CircularProgress size={32} />
        } else if (missionStatus === MissionStatus.Ongoing) {
            return (
                <ButtonStyle>
                    <ButtonText>
                        <StopMissionDialog mission={mission} />
                        <Typography variant="caption">{TranslateText('Stop')}</Typography>
                    </ButtonText>
                    <ButtonText>
                        <Button variant="ghost_icon" onClick={() => handleClick(ControlButton.Pause, mission)}>
                            <Icon
                                name={Icons.PauseButton}
                                style={{ color: tokens.colors.interactive.secondary__resting.rgba }}
                                size={40}
                            />
                        </Button>
                        <Typography variant="caption">{TranslateText('Pause')}</Typography>
                    </ButtonText>
                </ButtonStyle>
            )
        } else if (missionStatus === MissionStatus.Paused) {
            return (
                <ButtonStyle>
                    <ButtonText>
                        <StopMissionDialog mission={mission} />
                        <Typography variant="caption">{TranslateText('Stop')}</Typography>
                    </ButtonText>
                    <ButtonText>
                        <Button variant="ghost_icon" onClick={() => handleClick(ControlButton.Resume, mission)}>
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
