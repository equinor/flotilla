import { Button, Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Header } from 'components/Header/Header'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { RobotStatus, RobotWithoutTelemetry } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { VideoStreamSection, FieldLabel } from 'components/Styles/StyledComponents'
import { DocumentationSection } from './Documentation'
import { useMediaStreamContext } from 'components/Contexts/MediaStreamContext'
import { useContext, useEffect, useState } from 'react'
import { VideoStreamWindow } from '../MissionPage/VideoStream/VideoStreamWindow'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { SkipMissionDialog } from '../FrontPage/MissionOverview/StopDialogs'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { ReturnHomeButton } from './ReturnHomeButton'
import { phone_width } from 'utils/constants'
import { InterventionNeededButton } from './InterventionNeededButton'
import { useQuery } from '@tanstack/react-query'
import { useRobotTelemetry } from 'hooks/useRobotTelemetry'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { useAlertContext } from 'components/Contexts/AlertContext'

const StyledRobotPage = styled.div`
    display: flex;
    flex-direction: column;
    background-color: ${tokens.colors.ui.background__default.hex};
    min-height: 100vh;
`

const HeroSection = styled.div`
    display: flex;
    align-items: center;
    gap: 3rem;
    padding: 2rem 4rem;
    background-color: ${tokens.colors.ui.background__light.hex};
    box-sizing: border-box;
    @media (max-width: ${phone_width}) {
        flex-direction: column;
        padding: 1.5rem;
        gap: 16px;
        align-items: flex-start;
    }
`

const HeroLeft = styled.div`
    display: flex;
    flex-direction: column;
    gap: 16px;
`

const MetricsRow = styled.div`
    display: flex;
    flex-wrap: wrap;
    padding: 2rem 4rem;
    @media (max-width: ${phone_width}) {
        flex-direction: column;
        gap: 1rem;
        padding: 1rem 1.5rem;
    }
`

const MetricCard = styled.div`
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-width: 140px;
    padding: 0 2rem;
    border-left: 1px solid ${tokens.colors.ui.background__medium.hex};
    &:first-child {
        padding-left: 0;
        border-left: none;
    }
    @media (max-width: ${phone_width}) {
        padding: 0;
        border-left: none;
    }
`

const ActionsRow = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    padding: 0 4rem 2rem 4rem;
    @media (max-width: ${phone_width}) {
        padding: 0 1.5rem 1.5rem 1.5rem;
        flex-direction: column;
    }
`

interface RobotPageProps {
    robot: RobotWithoutTelemetry
}

export const RobotPage = ({ robot }: RobotPageProps) => {
    const { TranslateText } = useLanguageContext()
    const { mediaStreams, addMediaStreamConfigIfItDoesNotExist } = useMediaStreamContext()
    const { ongoingMissions } = useMissionsContext()
    const { robotBatteryLevel, robotBatteryStatus, robotPressureLevel } = useRobotTelemetry(robot)
    const backendApi = useBackendApi()
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)

    useEffect(() => {
        if (robot.id && !Object.keys(mediaStreams).includes(robot.id)) addMediaStreamConfigIfItDoesNotExist(robot.id)
    }, [robot.id])

    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const toggleSkipMissionDialog = () => {
        setIsDialogOpen(!isDialogOpen)
    }

    const mission = ongoingMissions.find((mission) => mission.robot.id === robot.id)

    const videoMediaStreams = (robot.id ? mediaStreams[robot.id]?.streams : undefined) ?? []

    const stopButton =
        robot && [RobotStatus.Busy, RobotStatus.Paused].includes(robot.status) ? (
            <Button variant="contained" onClick={toggleSkipMissionDialog}>
                <Icon
                    name={Icons.StopButton}
                    style={{ color: tokens.colors.interactive.icon_on_interactive_colors.rgba }}
                    size={24}
                />
                {TranslateText('Stop')} {robot.name}
            </Button>
        ) : (
            <></>
        )

    const skipMissionDialog =
        robot && [RobotStatus.Busy, RobotStatus.Paused].includes(robot.status) ? (
            <SkipMissionDialog
                missionName={mission?.name}
                robotId={robot.id}
                isSkipMissionDialogOpen={isDialogOpen}
                toggleDialog={toggleSkipMissionDialog}
            />
        ) : (
            <></>
        )

    const currentInspectionArea = useQuery({
        queryKey: ['fetchCurrentInspectionArea', robot.id],
        queryFn: async () => {
            if (robot && robot.currentInspectionAreaId)
                return await backendApi.getInspectionAreaById(robot.currentInspectionAreaId)
            return null
        },
        retry: 2,
        retryDelay: 2000,
        enabled: robot && robot.currentInspectionAreaId != null,
    }).data

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <StyledRobotPage>
                {robot && (
                    <>
                        <HeroSection>
                            <RobotImage height="280px" robotType={robot.type} />
                            <HeroLeft>
                                <Typography variant="h1">{robot.name}</Typography>
                                <RobotStatusChip status={robot.status} itemSize={24} />
                            </HeroLeft>
                        </HeroSection>

                        {robot.status !== RobotStatus.Offline && (
                            <MetricsRow>
                                <MetricCard>
                                    <FieldLabel>{TranslateText('Battery')}</FieldLabel>
                                    <BatteryStatusDisplay
                                        itemSize={24}
                                        batteryLevel={robotBatteryLevel}
                                        batteryState={robotBatteryStatus}
                                    />
                                </MetricCard>
                                {robotPressureLevel !== undefined && (
                                    <MetricCard>
                                        <FieldLabel>{TranslateText('Pressure')}</FieldLabel>
                                        <PressureStatusDisplay itemSize={24} pressure={robotPressureLevel} />
                                    </MetricCard>
                                )}
                                {robot.type && (
                                    <MetricCard>
                                        <FieldLabel>{TranslateText('Robot Model')}</FieldLabel>
                                        <Typography style={{ fontSize: '24px' }}>{robot.type}</Typography>
                                    </MetricCard>
                                )}
                                {currentInspectionArea && (
                                    <MetricCard>
                                        <FieldLabel>{TranslateText('Current Inspection Area')}</FieldLabel>
                                        <Typography style={{ fontSize: '24px' }}>
                                            {currentInspectionArea.inspectionAreaName}
                                        </Typography>
                                    </MetricCard>
                                )}
                            </MetricsRow>
                        )}

                        <ActionsRow>
                            {stopButton}
                            {robot.status != RobotStatus.InterventionNeeded && <ReturnHomeButton robot={robot} />}
                            {robot.status == RobotStatus.InterventionNeeded && (
                                <InterventionNeededButton robot={robot} />
                            )}
                            <MaintenanceButton robotId={robot.id} robotStatus={robot.status} />
                        </ActionsRow>

                        {skipMissionDialog}
                        {robot.documentation && robot.documentation.length > 0 && (
                            <DocumentationSection documentation={robot.documentation} />
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
    const backendApi = useBackendApi()

    const onSetMaintenenceMode = () => {
        setIsStoppingDueToMaintenance(true)

        backendApi
            .setMaintenanceMode(robotId)
            .then(() => {
                setIsStoppingDueToMaintenance(false)
            })
            .catch(() => {
                console.log(`Unable to set maintenance mode on robot with id ${robotId}. `)
                setIsStoppingDueToMaintenance(false)
            })
    }

    const onReleaseMaintenanceMode = () => {
        backendApi
            .releaseMaintenanceMode(robotId)
            .then(() => {})
            .catch(() => {
                console.log(`Unable to release maintenance mode on robot with id ${robotId}. `)
            })
    }

    return (
        <>
            {robotStatus == RobotStatus.Maintenance ? (
                <Button onClick={onReleaseMaintenanceMode}>{TranslateText(`Release maintenance mode`)}</Button>
            ) : (
                <Button color="danger" disabled={isStoppingDueToMaintenance} onClick={onSetMaintenenceMode}>
                    {TranslateText('Set maintenance mode')}
                </Button>
            )}
        </>
    )
}
