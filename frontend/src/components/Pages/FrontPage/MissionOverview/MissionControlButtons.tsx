import { Mission, MissionStatus } from 'models/Mission'
import { Button, CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { StopMissionDialog, MissionStatusRequest } from './StopDialogs'

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

export function MissionControlButtons({ mission }: MissionProps) {
    const { TranslateText } = useLanguageContext()
    const { missionControlState, updateMissionState } = useMissionControlContext()

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
                        <Button
                            variant="ghost_icon"
                            onClick={() => updateMissionState(MissionStatusRequest.Pause, mission)}
                        >
                            <Icon
                                name={Icons.PauseButton}
                                style={{ color: tokens.colors.interactive.secondary__resting.hex }}
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
                        <Button
                            variant="ghost_icon"
                            onClick={() => updateMissionState(MissionStatusRequest.Resume, mission)}
                        >
                            <Icon
                                name={Icons.PlayButton}
                                style={{ color: tokens.colors.interactive.secondary__resting.hex }}
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
    return <>{renderControlIcon(mission.status)}</>
}
