import { Button, Card as EdsCard, Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Link } from 'react-router-dom'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { getRobotTypeString, RobotStatus, RobotWithoutTelemetry } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { VideoStreamSection, FieldLabel, cardShadow } from 'components/Styles/StyledComponents'
import { DocumentationSection } from './Documentation'
import { RobotStatisticsSection } from './RobotStatistics/RobotStatisticsSection'
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
    gap: 2rem;
    padding: 2rem 3rem;
    box-sizing: border-box;
    background-color: ${tokens.colors.ui.background__light.hex};
    min-height: calc(100vh - 65px);
    @media (max-width: ${phone_width}) {
        padding: 1.25rem;
        gap: 1.5rem;
    }
`

const Breadcrumb = styled.div`
    display: flex;
    align-items: center;
    gap: 6px;
    font-family: Equinor, sans-serif;
    font-size: 0.72rem;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`

const BreadcrumbLink = styled(Link)`
    color: ${tokens.colors.text.static_icons__tertiary.hex};
    text-decoration: none;
    &:hover {
        color: ${tokens.colors.interactive.primary__resting.hex};
    }
`

const BreadcrumbCurrent = styled.span`
    color: ${tokens.colors.text.static_icons__default.hex};
`

const Card = styled(EdsCard)`
    box-shadow: ${cardShadow};
    box-sizing: border-box;
`

const HeroCard = styled(Card)`
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 2.5rem;
    padding: 1.75rem 2rem;
    border-left: 4px solid ${tokens.colors.interactive.primary__resting.hex};
    @media (max-width: ${phone_width}) {
        flex-direction: column;
        align-items: flex-start;
        gap: 1.25rem;
        padding: 1.25rem;
    }
`

const HeroInfo = styled.div`
    display: flex;
    flex-direction: column;
    gap: 12px;
`

const RobotTypeText = styled(Typography)`
    font-size: 0.9rem;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`

const HeroSpacer = styled.div`
    flex: 1 1 auto;
`

const HeroActions = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    @media (max-width: ${phone_width}) {
        width: 100%;
    }
`

const MetricsCard = styled(Card)`
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    padding: 1.5rem 2rem;
    @media (max-width: ${phone_width}) {
        flex-direction: column;
        gap: 1.25rem;
        padding: 1.25rem;
    }
`

const MetricColumn = styled.div`
    display: flex;
    flex-direction: column;
    gap: 10px;
    flex: 1 1 0;
    min-width: 150px;
    padding: 0 1.75rem;
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
            <NavBar />
            <StyledRobotPage>
                {robot && (
                    <>
                        <Breadcrumb>
                            <BreadcrumbLink to={`/${installation?.installationCode}/mission-control`}>
                                {TranslateText('Mission Control')}
                            </BreadcrumbLink>
                            <Icon name={Icons.RightCheveron} size={16} />
                            <BreadcrumbCurrent>{robot.name}</BreadcrumbCurrent>
                        </Breadcrumb>

                        <HeroCard>
                            <RobotImage height="180px" robotType={robot.type} />
                            <HeroInfo>
                                <Typography variant="h1">{robot.name}</Typography>
                                <RobotStatusChip status={robot.status} itemSize={24} />
                                {robot.type && <RobotTypeText>{getRobotTypeString(robot.type)}</RobotTypeText>}
                            </HeroInfo>
                            <HeroSpacer />
                            <HeroActions>
                                {stopButton}
                                {robot.status != RobotStatus.InterventionNeeded && <ReturnHomeButton robot={robot} />}
                                {robot.status == RobotStatus.InterventionNeeded && (
                                    <InterventionNeededButton robot={robot} />
                                )}
                                <MaintenanceButton robotId={robot.id} robotStatus={robot.status} />
                            </HeroActions>
                        </HeroCard>

                        {robot.status !== RobotStatus.Offline && (
                            <MetricsCard>
                                <MetricColumn>
                                    <FieldLabel>{TranslateText('Battery')}</FieldLabel>
                                    <BatteryStatusDisplay
                                        itemSize={24}
                                        batteryLevel={robotBatteryLevel}
                                        batteryState={robotBatteryStatus}
                                    />
                                </MetricColumn>
                                {robotPressureLevel !== undefined && (
                                    <MetricColumn>
                                        <FieldLabel>{TranslateText('Pressure')}</FieldLabel>
                                        <PressureStatusDisplay itemSize={24} pressure={robotPressureLevel} />
                                    </MetricColumn>
                                )}
                                {robot.type && (
                                    <MetricColumn>
                                        <FieldLabel>{TranslateText('Robot Model')}</FieldLabel>
                                        <Typography style={{ fontSize: '24px' }}>{robot.type}</Typography>
                                    </MetricColumn>
                                )}
                                {currentInspectionArea && (
                                    <MetricColumn>
                                        <FieldLabel>{TranslateText('Current Inspection Area')}</FieldLabel>
                                        <Typography style={{ fontSize: '24px' }}>
                                            {currentInspectionArea.inspectionAreaName}
                                        </Typography>
                                    </MetricColumn>
                                )}
                            </MetricsCard>
                        )}

                        <RobotStatisticsSection robotId={robot.id} />

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
