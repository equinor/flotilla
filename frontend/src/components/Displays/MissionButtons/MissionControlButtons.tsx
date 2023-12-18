import { MissionStatus } from 'models/Mission'
import { Button, CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { StopMissionDialog, MissionStatusRequest } from 'components/Pages/FrontPage/MissionOverview/StopDialogs'

interface MissionControlButtonsProps {
    missionName: string
    robotId: string
    missionStatus: MissionStatus
}

interface MissionProps {
    missionName: string
    robotId: string
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

export const MissionControlButtons = ({ missionName, robotId, missionStatus }: MissionControlButtonsProps) => {
    const { missionControlState } = useMissionControlContext()

    return (
        <>
            {missionControlState.isWaitingForResponse ? (
                <CircularProgress size={32} />
            ) : (
                <>
                    {missionStatus === MissionStatus.Ongoing && (
                        <OngoingMissionButton missionName={missionName} robotId={robotId} />
                    )}
                    {missionStatus === MissionStatus.Paused && (
                        <PausedMissionButton missionName={missionName} robotId={robotId} />
                    )}
                </>
            )}
        </>
    )
}

const OngoingMissionButton = ({ missionName, robotId }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { updateRobotMissionState } = useMissionControlContext()

    return (
        <>
            <ButtonStyle>
                <ButtonText>
                    <StopMissionDialog missionName={missionName} robotId={robotId} />
                    <Typography variant="caption">{TranslateText('Stop')}</Typography>
                </ButtonText>
                <ButtonText>
                    <Button
                        variant="ghost_icon"
                        onClick={() => updateRobotMissionState(MissionStatusRequest.Pause, robotId)}
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

const PausedMissionButton = ({ missionName, robotId }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { updateRobotMissionState } = useMissionControlContext()

    return (
        <>
            <ButtonStyle>
                <ButtonText>
                    <StopMissionDialog missionName={missionName} robotId={robotId} />
                    <Typography variant="caption">{TranslateText('Stop')}</Typography>
                </ButtonText>
                <ButtonText>
                    <Button
                        variant="ghost_icon"
                        onClick={() => updateRobotMissionState(MissionStatusRequest.Resume, robotId)}
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
