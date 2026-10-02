import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMissionsContext } from 'contexts/MissionRunsContext'
import { RobotWithoutTelemetry, RobotStatus } from 'models/Robot'
import { MissionStatus } from 'models/Mission'
import { MissionProgressDisplay } from 'components/Displays/MissionDisplays/MissionProgressDisplay'
import { MissionStatusDisplayWithHeader } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { NoMissionReason } from 'utils/IsRobotReadyToRunMissions'

const StyledMissionStatus = styled.div`
    display: flex;
    flex-direction: column;
    flex: 1;
    padding: 16px;
    gap: 16px;
`
const Midcontent = styled.div`
    display: flex;
    align-items: flex-start;
    gap: 24px;
`

interface DashboardMissionStatusProps {
    robot: RobotWithoutTelemetry
}

export const DashboardMissionStatus = ({ robot }: DashboardMissionStatusProps) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions } = useMissionsContext()
    const ongoingMission = ongoingMissions.find((mission) => mission.robot.id === robot.id)

    let missionName: string | undefined
    let missionStatus: MissionStatus | undefined
    let progressMission: typeof ongoingMission

    switch (robot.status) {
        case RobotStatus.ReturningHome:
            missionName = TranslateText('Return robot to home')
            missionStatus = MissionStatus.Ongoing
            break
        case RobotStatus.ReturnHomePaused:
            missionName = TranslateText('Return robot to home')
            missionStatus = MissionStatus.Paused
            break
        case RobotStatus.GoingToLockdown:
        case RobotStatus.GoingToRecharging:
            missionName = TranslateText('Return robot to home')
            missionStatus = MissionStatus.Ongoing
            break
        case RobotStatus.Paused:
        case RobotStatus.Busy:
        case RobotStatus.Stopping:
        case RobotStatus.Pausing:
        case RobotStatus.StoppingReturnHome:
            if (ongoingMission) {
                missionName = ongoingMission.name
                missionStatus = ongoingMission.status
                progressMission = ongoingMission
            }
            break
        case RobotStatus.RechargingWithMission:
        case RobotStatus.GoingToRechargingWithMission:
            if (ongoingMission) {
                missionName = ongoingMission.name
                missionStatus = MissionStatus.PausedToCharge
                progressMission = ongoingMission
            }
            break
        default:
            break
    }

    if (!missionName || !missionStatus) {
        return (
            <StyledMissionStatus>
                <Typography variant="h5">{TranslateText('No ongoing missions')}</Typography>
                <NoMissionReason robot={robot} />
            </StyledMissionStatus>
        )
    }

    return (
        <StyledMissionStatus>
            <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                {missionName}
            </Typography>
            <Midcontent>
                <MissionStatusDisplayWithHeader status={missionStatus} />
                {progressMission && <MissionProgressDisplay mission={progressMission} />}
            </Midcontent>
        </StyledMissionStatus>
    )
}
