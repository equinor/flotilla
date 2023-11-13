import { Card, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { MissionProgressDisplay } from 'components/Displays/MissionDisplays/MissionProgressDisplay'
import { MissionStatusDisplayWithHeader } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { MissionControlButtons } from 'components/Displays/MissionButtons/MissionControlButtons'
import BatteryStatusDisplay from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { BatteryStatus } from 'models/Battery'
import { MissionRobotDisplay } from 'components/Displays/MissionDisplays/MissionRobotDisplay'

interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 400px;
    height: 140px;
    padding: 10px;
    justify-content: space-between;
`
const StyledTitle = styled(Card)`
    width: 70%;
    height: 50px;
    justify-content: center;
    padding-left: 12px;
    :hover {
        background-color: #deedee;
    }
    box-shadow: none;
`
const TopContent = styled.div`
    display: flex;
    justify-content: space-between;
`
const BottomContent = styled.div`
    display: flex;
    justify-content: space-between;
`
export function OngoingMissionCard({ mission }: MissionProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }
    return (
        <StyledMissionCard style={{ boxShadow: tokens.elevation.raised }}>
            <TopContent>
                <StyledTitle onClick={routeChange}>
                    <Typography variant="h6" color="primary">
                        {mission.name}
                    </Typography>
                </StyledTitle>
                <MissionControlButtons mission={mission} />
            </TopContent>
            <BottomContent>
                <MissionStatusDisplayWithHeader status={mission.status} />
                <MissionProgressDisplay mission={mission} />
                <MissionRobotDisplay mission={mission} />
                <BatteryStatusDisplay
                    battery={mission.robot.batteryLevel}
                    batteryStatus={BatteryStatus.Normal}
                    robotStatus={mission.robot.status}
                />
            </BottomContent>
        </StyledMissionCard>
    )
}
