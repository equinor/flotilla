import { Icon, Typography } from '@equinor/eds-core-react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { PressureTable } from './PressureTable'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { RobotStatus } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotType } from 'models/RobotModel'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { StyledButton, StyledPage } from 'components/Styles/StyledComponents'
import { DocumentationSection } from './Documentation'
import { useMediaStreamContext } from 'components/Contexts/MediaStreamContext'
import { VideoStreamSection } from '../MissionPage/MissionPage'
import { useEffect, useState } from 'react'
import { VideoStreamWindow } from '../MissionPage/VideoStream/VideoStreamWindow'
import { MoveRobotArmSection } from './RobotArmMovement'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { StopMissionDialog } from '../FrontPage/MissionOverview/StopDialogs'
import { TaskType } from 'models/Task'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { ReturnHomeButton } from './ReturnHomeButton'

const StyledTextButton = styled(StyledButton)`
    text-align: left;
    max-width: 12rem;
`
const RobotInfo = styled.div`
    display: flex;
    flex-direction: row;
    align-items: flex-start;
    gap: 3rem;
    width: calc(80vw);
    @media (max-width: 600px) {
        flex-direction: column;
    }
    margin: 0rem 0rem 2rem 0rem;
`
const StatusContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: start;
    justify-content: flex-end;
    gap: 2rem;
    @media (max-width: 600px) {
        align-items: flex-start;
    }
`

export const RobotPage = () => {
    const { TranslateText } = useLanguageContext()
    const { robotId } = useParams()
    const { enabledRobots } = useRobotContext()
    const { mediaStreams, addMediaStreamConfigIfItDoesNotExist } = useMediaStreamContext()
    const [videoMediaStreams, setVideoMediaStreams] = useState<MediaStreamTrack[]>([])
    const { ongoingMissions } = useMissionsContext()

    useEffect(() => {
        if (robotId && !Object.keys(mediaStreams).includes(robotId)) addMediaStreamConfigIfItDoesNotExist(robotId)
    }, [robotId])

    const selectedRobot = enabledRobots.find((robot) => robot.id === robotId)

    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const toggleStopMissionDialog = () => {
        setIsDialogOpen(!isDialogOpen)
    }

    const mission = ongoingMissions.find((mission) => mission.robot.id === selectedRobot?.id)

    let missionTaskType = undefined
    if (mission?.tasks.every((task) => task.type === TaskType.Inspection)) missionTaskType = TaskType.Inspection
    if (mission?.tasks.every((task) => task.type === TaskType.ReturnHome)) missionTaskType = TaskType.ReturnHome
    if (mission?.tasks.every((task) => task.type === TaskType.Localization)) missionTaskType = TaskType.Localization

    useEffect(() => {
        if (robotId && mediaStreams && Object.keys(mediaStreams).includes(robotId)) {
            const mediaStreamConfig = mediaStreams[robotId]
            if (mediaStreamConfig && mediaStreamConfig.streams.length > 0)
                setVideoMediaStreams(mediaStreamConfig.streams)
        }
    }, [mediaStreams, robotId])

    return (
        <>
            <Header page={'robot'} />
            <StyledPage>
                <BackButton />
                {selectedRobot && (
                    <>
                        <Typography variant="h1">
                            {selectedRobot.name + ' (' + selectedRobot.model.type + ')'}
                        </Typography>
                        <RobotInfo>
                            <RobotImage height="350px" robotType={selectedRobot.model.type} />
                            <StatusContent>
                                <RobotStatusChip
                                    status={selectedRobot.status}
                                    flotillaStatus={selectedRobot.flotillaStatus}
                                    isarConnected={selectedRobot.isarConnected}
                                    itemSize={32}
                                />

                                {selectedRobot.status !== RobotStatus.Offline && (
                                    <>
                                        <BatteryStatusDisplay
                                            itemSize={32}
                                            batteryLevel={selectedRobot.batteryLevel}
                                            batteryState={selectedRobot.batteryState}
                                            batteryWarningLimit={selectedRobot.model.batteryWarningThreshold}
                                        />
                                        {selectedRobot.pressureLevel !== null &&
                                            selectedRobot.pressureLevel !== undefined && (
                                                <PressureStatusDisplay
                                                    itemSize={32}
                                                    pressure={selectedRobot.pressureLevel}
                                                    upperPressureWarningThreshold={
                                                        selectedRobot.model.upperPressureWarningThreshold
                                                    }
                                                    lowerPressureWarningThreshold={
                                                        selectedRobot.model.lowerPressureWarningThreshold
                                                    }
                                                />
                                            )}
                                    </>
                                )}
                            </StatusContent>
                        </RobotInfo>
                        {selectedRobot.model.type === RobotType.TaurobInspector && <PressureTable />}
                        <Typography variant="h2">{TranslateText('Actions')}</Typography>

                        <StyledTextButton
                            variant="contained"
                            onClick={() => {
                                toggleStopMissionDialog()
                            }}
                        >
                            <Icon
                                name={Icons.StopButton}
                                style={{ color: tokens.colors.interactive.icon_on_interactive_colors.rgba }}
                                size={24}
                            />
                            {TranslateText('Stop')} {selectedRobot.name}
                            <StopMissionDialog
                                missionName={mission?.name}
                                robotId={selectedRobot.id}
                                missionTaskType={missionTaskType}
                                isStopMissionDialogOpen={isDialogOpen}
                                toggleDialog={toggleStopMissionDialog}
                            />
                        </StyledTextButton>
                        {selectedRobot && <ReturnHomeButton robot={selectedRobot} />}

                        {selectedRobot.model.type === RobotType.TaurobInspector && (
                            <MoveRobotArmSection robot={selectedRobot} />
                        )}
                        {selectedRobot.documentation && selectedRobot.documentation.length > 0 && (
                            <DocumentationSection documentation={selectedRobot.documentation} />
                        )}
                        <VideoStreamSection>
                            {videoMediaStreams && videoMediaStreams.length > 0 && (
                                <VideoStreamWindow videoStreams={videoMediaStreams} />
                            )}
                        </VideoStreamSection>
                    </>
                )}
            </StyledPage>
        </>
    )
}
