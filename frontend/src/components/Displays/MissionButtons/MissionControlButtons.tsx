import { MissionStatus } from 'models/Mission'
import { Button, CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { SkipMissionDialog, MissionStatusRequest } from 'pages/FrontPage/MissionOverview/StopDialogs'
import { useState } from 'react'

interface MissionControlButtonsProps {
    missionName: string
    robotId: string
    missionStatus: MissionStatus
    canBePaused: boolean
    canBeSkipped: boolean
}

interface MissionProps {
    missionName: string
    robotId: string
    canBePaused: boolean
    canBeSkipped: boolean
}

const ButtonStyle = styled.div`
    display: flex;
    height: 56px;
    align-items: flex-start;
    gap: 16px;
`
const ButtonText = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
`
const ButtonIcon = styled(Button)`
    display: flex;
    width: 35px;
    height: 35px;
    justify-content: center;
    align-items: center;
    gap: 8px;
`

export const MissionControlButtons = ({
    missionName,
    robotId,
    missionStatus,
    canBePaused,
    canBeSkipped,
}: MissionControlButtonsProps) => {
    const { missionControlState } = useMissionControlContext()

    return (
        <>
            {missionControlState.isRobotMissionWaitingForResponseDict[robotId] ? (
                <CircularProgress size={32} />
            ) : (
                <>
                    {missionStatus === MissionStatus.Ongoing && (
                        <OngoingMissionButton
                            missionName={missionName}
                            robotId={robotId}
                            canBePaused={canBePaused}
                            canBeSkipped={canBeSkipped}
                        />
                    )}
                    {missionStatus === MissionStatus.Paused && (
                        <PausedMissionButton
                            missionName={missionName}
                            robotId={robotId}
                            canBePaused={true}
                            canBeSkipped={canBeSkipped}
                        />
                    )}
                </>
            )}
        </>
    )
}

const OngoingMissionButton = ({ missionName, robotId, canBePaused, canBeSkipped }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { updateRobotMissionState } = useMissionControlContext()
    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const toggleSkipMissionDialog = () => {
        setIsDialogOpen(!isDialogOpen)
    }

    const skipMissionButton = (
        <ButtonText onClick={toggleSkipMissionDialog}>
            <ButtonIcon variant="contained_icon">
                <Icon name={Icons.Next} size={24} />
            </ButtonIcon>
            <Typography variant="caption">{TranslateText('Skip')}</Typography>
            <SkipMissionDialog
                missionName={missionName}
                robotId={robotId}
                isSkipMissionDialogOpen={isDialogOpen}
                toggleDialog={toggleSkipMissionDialog}
            />
        </ButtonText>
    )

    const pauseMissionButton = (
        <ButtonText onClick={() => updateRobotMissionState(MissionStatusRequest.Pause, robotId)}>
            <ButtonIcon variant="contained_icon">
                <Icon name={Icons.PauseStandard} size={24} />
            </ButtonIcon>
            <Typography variant="caption">{TranslateText('Pause')}</Typography>
        </ButtonText>
    )

    return (
        <ButtonStyle>
            {canBePaused && pauseMissionButton}
            {canBeSkipped && skipMissionButton}
        </ButtonStyle>
    )
}

const PausedMissionButton = ({ missionName, robotId, canBeSkipped }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { updateRobotMissionState } = useMissionControlContext()
    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const toggleSkipMissionDialog = () => {
        setIsDialogOpen(!isDialogOpen)
    }

    const skipMissionButton = (
        <ButtonText onClick={toggleSkipMissionDialog}>
            <ButtonIcon variant="contained_icon">
                <Icon name={Icons.Next} size={24} />
            </ButtonIcon>
            <Typography variant="caption">{TranslateText('Skip')}</Typography>
            <SkipMissionDialog
                missionName={missionName}
                robotId={robotId}
                isSkipMissionDialogOpen={isDialogOpen}
                toggleDialog={toggleSkipMissionDialog}
            />
        </ButtonText>
    )

    return (
        <ButtonStyle>
            <ButtonText onClick={() => updateRobotMissionState(MissionStatusRequest.Resume, robotId)}>
                <ButtonIcon variant="contained_icon">
                    <Icon name={Icons.PlayStandard} size={24} />
                </ButtonIcon>
                <Typography variant="caption">{TranslateText('Start')}</Typography>
            </ButtonText>
            {canBeSkipped && skipMissionButton}
        </ButtonStyle>
    )
}
