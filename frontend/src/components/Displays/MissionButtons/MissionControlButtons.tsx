import { MissionStatus } from 'models/Mission'
import { Button, CircularProgress, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionControlContext } from 'components/Contexts/MissionControlContext'
import { SkipMissionDialog, MissionStatusRequest } from 'components/Pages/FrontPage/MissionOverview/StopDialogs'
import { TaskType } from 'models/Task'
import { useState } from 'react'

interface MissionControlButtonsProps {
    missionName: string
    robotId: string
    missionStatus: MissionStatus
    missionTaskType: TaskType
}

interface MissionProps {
    missionName: string
    robotId: string
    missionTaskType: TaskType
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
    missionTaskType,
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
                            missionTaskType={missionTaskType}
                        />
                    )}
                    {missionStatus === MissionStatus.Paused && (
                        <PausedMissionButton
                            missionName={missionName}
                            robotId={robotId}
                            missionTaskType={missionTaskType}
                        />
                    )}
                </>
            )}
        </>
    )
}

const OngoingMissionButton = ({ missionName, robotId, missionTaskType }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { updateRobotMissionState } = useMissionControlContext()
    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const toggleSkipMissionDialog = () => {
        setIsDialogOpen(!isDialogOpen)
    }

    return (
        <ButtonStyle>
            <ButtonText onClick={() => updateRobotMissionState(MissionStatusRequest.Pause, robotId)}>
                <ButtonIcon variant="contained_icon">
                    <Icon name={Icons.PauseStandard} size={24} />
                </ButtonIcon>
                <Typography variant="caption">{TranslateText('Pause')}</Typography>
            </ButtonText>
            <ButtonText onClick={toggleSkipMissionDialog}>
                <ButtonIcon variant="contained_icon">
                    <Icon name={Icons.Next} size={24} />
                </ButtonIcon>
                <Typography variant="caption">{TranslateText('Skip')}</Typography>
                <SkipMissionDialog
                    missionName={missionName}
                    robotId={robotId}
                    missionTaskType={missionTaskType}
                    isSkipMissionDialogOpen={isDialogOpen}
                    toggleDialog={toggleSkipMissionDialog}
                />
            </ButtonText>
        </ButtonStyle>
    )
}

const PausedMissionButton = ({ missionName, robotId, missionTaskType }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const { updateRobotMissionState } = useMissionControlContext()
    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const toggleSkipMissionDialog = () => {
        setIsDialogOpen(!isDialogOpen)
    }

    return (
        <ButtonStyle>
            <ButtonText onClick={() => updateRobotMissionState(MissionStatusRequest.Resume, robotId)}>
                <ButtonIcon variant="contained_icon">
                    <Icon name={Icons.PlayStandard} size={24} />
                </ButtonIcon>
                <Typography variant="caption">{TranslateText('Start')}</Typography>
            </ButtonText>
            <ButtonText onClick={toggleSkipMissionDialog}>
                <ButtonIcon variant="contained_icon">
                    <Icon name={Icons.Next} size={24} />
                </ButtonIcon>
                <Typography variant="caption">{TranslateText('Skip')}</Typography>
                <SkipMissionDialog
                    missionName={missionName}
                    robotId={robotId}
                    missionTaskType={missionTaskType}
                    isSkipMissionDialogOpen={isDialogOpen}
                    toggleDialog={toggleSkipMissionDialog}
                />
            </ButtonText>
        </ButtonStyle>
    )
}
