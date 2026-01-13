import { Icon, Paper, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { RobotStatus } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { StyledButton, StyledPage } from 'components/Styles/StyledComponents'
import { DocumentationSection } from './Documentation'
import { useMediaStreamContext } from 'components/Contexts/MediaStreamContext'
import { VideoStreamSection } from '../MissionPage/MissionPage'
import { useEffect, useState } from 'react'
import { VideoStreamWindow } from '../MissionPage/VideoStream/VideoStreamWindow'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { SkipMissionDialog } from '../FrontPage/MissionOverview/StopDialogs'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { ReturnHomeButton } from './ReturnHomeButton'
import { phone_width } from 'utils/constants'
import { InterventionNeededButton } from './InterventionNeededButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { useQuery } from '@tanstack/react-query'

const StyledRobotPage = styled(StyledPage)`
    background-color: ${tokens.colors.ui.background__light.hex};
    gap: 5px;
`
const FullWidthButton = styled(StyledButton)`
    text-align: left;
    align-self: stretch;
`
const RobotInfo = styled.div`
    display: flex;
    align-items: center;
    gap: 32px;
    align-self: stretch;
    width: 100%;
    @media (max-width: ${phone_width}) {
        flex-direction: column;
    }
`
const StatusContent = styled.div`
    gap: 48px;
    display: grid;
    grid-template-columns: auto auto auto;
    align-self: start;
    @media (max-width: ${phone_width}) {
        align-items: flex-start;
        grid-template-columns: repeat(1, 70vw);
        flex-direction: column;
        gap: 8px;
    }
`
const StyledContainer = styled(Paper)`
    padding: 24px;
    width: fit-content;
    @media (max-width: ${phone_width}) {
        width: 90vw;
    }
`

const StyledLeftContent = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
    gap: 8px;
    align-self: stretch;
`

const StyledStatusElement = styled.div`
    display: flex;
    padding: 2px 6px 4px 6px;
    flex-direction: column;
    align-items: flex-start;
`
const StyledWideItem = styled(StyledStatusElement)`
    grid-column: span 2;
    @media (max-width: ${phone_width}) {
        grid-column: span 1;
    }
`

export const RobotPage = ({ robotId }: { robotId: string }) => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { mediaStreams, addMediaStreamConfigIfItDoesNotExist } = useMediaStreamContext()
    const [videoMediaStreams, setVideoMediaStreams] = useState<MediaStreamTrack[]>([])
    const { ongoingMissions } = useMissionsContext()

    useEffect(() => {
        if (robotId && !Object.keys(mediaStreams).includes(robotId)) addMediaStreamConfigIfItDoesNotExist(robotId)
    }, [robotId])

    const selectedRobot = enabledRobots.find((robot) => robot.id === robotId)

    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const toggleSkipMissionDialog = () => {
        setIsDialogOpen(!isDialogOpen)
    }

    const mission = ongoingMissions.find((mission) => mission.robot.id === selectedRobot?.id)

    useEffect(() => {
        if (robotId && mediaStreams && Object.keys(mediaStreams).includes(robotId)) {
            const mediaStreamConfig = mediaStreams[robotId]
            if (mediaStreamConfig && mediaStreamConfig.streams.length > 0)
                setVideoMediaStreams(mediaStreamConfig.streams)
        }
    }, [mediaStreams, robotId])

    const stopButton =
        selectedRobot && [RobotStatus.Busy, RobotStatus.Paused].includes(selectedRobot.status) ? (
            <FullWidthButton variant="contained" onClick={toggleSkipMissionDialog}>
                <Icon
                    name={Icons.StopButton}
                    style={{ color: tokens.colors.interactive.icon_on_interactive_colors.rgba }}
                    size={24}
                />
                {TranslateText('Stop')} {selectedRobot.name}
            </FullWidthButton>
        ) : (
            <></>
        )

    const skipMissionDialog =
        selectedRobot && [RobotStatus.Busy, RobotStatus.Paused].includes(selectedRobot.status) ? (
            <SkipMissionDialog
                missionName={mission?.name}
                robotId={selectedRobot.id}
                isSkipMissionDialogOpen={isDialogOpen}
                toggleDialog={toggleSkipMissionDialog}
            />
        ) : (
            <></>
        )

    const currentInspectionArea = useQuery({
        queryKey: ['fetchCurrentInspectionArea', robotId],
        queryFn: async () => {
            if (selectedRobot && selectedRobot.currentInspectionAreaId)
                return await BackendAPICaller.getInspectionAreaById(selectedRobot.currentInspectionAreaId)
            return null
        },
        retry: 2,
        retryDelay: 2000,
        enabled: selectedRobot && selectedRobot.currentInspectionAreaId != null,
    }).data

    return (
        <>
            <Header page={'robot'} />
            <StyledRobotPage>
                <BackButton />
                {selectedRobot && (
                    <>
                        <StyledContainer>
                            <Typography variant="h1">{selectedRobot.name}</Typography>
                            <RobotInfo>
                                <StyledLeftContent>
                                    <RobotImage height="350px" robotType={selectedRobot.model.type} />
                                    {stopButton}
                                    {selectedRobot && selectedRobot.status != RobotStatus.InterventionNeeded && (
                                        <ReturnHomeButton robot={selectedRobot} />
                                    )}
                                    {selectedRobot && selectedRobot.status == RobotStatus.InterventionNeeded && (
                                        <InterventionNeededButton robot={selectedRobot} />
                                    )}
                                    {selectedRobot && (
                                        <MaintenanceButton
                                            robotId={selectedRobot.id}
                                            robotStatus={selectedRobot.status}
                                        />
                                    )}
                                </StyledLeftContent>
                                <StatusContent>
                                    <StyledStatusElement>
                                        <Typography variant="caption">{TranslateText('Status')}</Typography>
                                        <RobotStatusChip
                                            status={selectedRobot.status}
                                            isarConnected={selectedRobot.isarConnected}
                                            itemSize={24}
                                        />
                                    </StyledStatusElement>

                                    {selectedRobot.status !== RobotStatus.Offline && (
                                        <>
                                            <StyledStatusElement>
                                                <Typography variant="caption">{TranslateText('Battery')}</Typography>
                                                <BatteryStatusDisplay
                                                    itemSize={24}
                                                    batteryLevel={selectedRobot.batteryLevel}
                                                    batteryState={selectedRobot.batteryState}
                                                    batteryWarningLimit={selectedRobot.model.batteryWarningThreshold}
                                                />
                                            </StyledStatusElement>
                                            {selectedRobot.pressureLevel !== null &&
                                                selectedRobot.pressureLevel !== undefined && (
                                                    <StyledStatusElement>
                                                        <Typography variant="caption">
                                                            {TranslateText('Pressure')}
                                                        </Typography>
                                                        <PressureStatusDisplay
                                                            itemSize={24}
                                                            pressure={selectedRobot.pressureLevel}
                                                            upperPressureWarningThreshold={
                                                                selectedRobot.model.upperPressureWarningThreshold
                                                            }
                                                            lowerPressureWarningThreshold={
                                                                selectedRobot.model.lowerPressureWarningThreshold
                                                            }
                                                        />
                                                    </StyledStatusElement>
                                                )}
                                            {selectedRobot.model.type && (
                                                <StyledStatusElement>
                                                    <Typography variant="caption">
                                                        {TranslateText('Robot Model')}
                                                    </Typography>
                                                    <Typography style={{ fontSize: '24px' }}>
                                                        {selectedRobot.model.type}
                                                    </Typography>
                                                </StyledStatusElement>
                                            )}
                                            {currentInspectionArea && (
                                                <StyledWideItem>
                                                    <Typography variant="caption">
                                                        {TranslateText('Current Inspection Area')}
                                                    </Typography>
                                                    <Typography style={{ fontSize: '24px' }}>
                                                        {currentInspectionArea.inspectionAreaName}
                                                    </Typography>
                                                </StyledWideItem>
                                            )}
                                        </>
                                    )}
                                </StatusContent>
                            </RobotInfo>
                        </StyledContainer>
                        {skipMissionDialog}
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
            </StyledRobotPage>
        </>
    )
}

interface MaintenanceButtonProps {
    robotId: string
    robotStatus: RobotStatus
}

const MaintenanceButton = ({ robotId, robotStatus }: MaintenanceButtonProps) => {
    const { TranslateText } = useLanguageContext()
    const [isStoppingDueToMaintenance, setIsStoppingDueToMaintenance] = useState(false)

    const onSetMaintenenceMode = () => {
        setIsStoppingDueToMaintenance(true)

        BackendAPICaller.setMaintenanceMode(robotId)
            .then(() => {
                setIsStoppingDueToMaintenance(false)
            })
            .catch(() => {
                console.log(`Unable to set maintenance mode on robot with id ${robotId}. `)
                setIsStoppingDueToMaintenance(false)
            })
    }

    const onReleaseMaintenanceMode = () => {
        BackendAPICaller.releaseMaintenanceMode(robotId)
            .then(() => {})
            .catch(() => {
                console.log(`Unable to release maintenance mode on robot with id ${robotId}. `)
            })
    }

    return (
        <>
            {robotStatus == RobotStatus.Maintenance ? (
                <FullWidthButton onClick={onReleaseMaintenanceMode}>
                    {TranslateText(`Release maintenance mode`)}
                </FullWidthButton>
            ) : (
                <FullWidthButton color="danger" disabled={isStoppingDueToMaintenance} onClick={onSetMaintenenceMode}>
                    {TranslateText('Set maintenance mode')}
                </FullWidthButton>
            )}
        </>
    )
}
