import { Mission, MissionStatus } from 'models/Mission'
import { Button, CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { StopMissionDialog, MissionStatusRequest } from '../../Pages/FrontPage/MissionOverview/StopDialogs'

interface MissionProps {
    mission: Mission
}

const ButtonStyle = styled.div`
    display: grid;
    grid-template-columns: 45px 45px;
    margin-end: 20px;
`

const ButtonText = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
`

export function MissionControlButtons({ mission }: MissionProps) {
    const { missionControlState } = useMissionControlContext()

    return (
        <>
            {missionControlState.isWaitingForResponse ? (
                <CircularProgress size={32} />
            ) : (
                <>
                    {mission.status === MissionStatus.Ongoing && <OngoingMissionButton mission={mission} />}
                    {mission.status === MissionStatus.Paused && <PausedMissionButton mission={mission} />}
                </>
            )}
        </>
    )
}

function OngoingMissionButton({ mission }: MissionProps) {
    const { TranslateText } = useLanguageContext()
    const { updateMissionState } = useMissionControlContext()

    return (
        <>
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
        </>
    )
}

function PausedMissionButton({ mission }: MissionProps) {
    const { TranslateText } = useLanguageContext()
    const { updateMissionState } = useMissionControlContext()

    return (
        <>
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
        </>
    )
}
