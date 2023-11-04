import { Card, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { MissionProgressDisplay } from './MissionProgressDisplay'
import { MissionStatusDisplay } from './MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { MissionControlButtons } from './MissionControlButtons'
import BatteryStatusView from '../RobotCards/BatteryStatusView'
import { BatteryStatus } from 'models/Battery'
import { MissionRobotDisplay } from './MissionRobotDisplay'

interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 432px;
    padding: 10px;
`
const StyledTitle = styled(Card)`
    width: 70%;
    height: 80%;
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
                <MissionStatusDisplay status={mission.status} />
                <MissionProgressDisplay mission={mission} />
                <MissionRobotDisplay mission={mission} />
                <BatteryStatusView
                    battery={mission.robot.batteryLevel}
                    batteryStatus={BatteryStatus.Normal}
                    robotStatus={mission.robot.status}
                />
            </BottomContent>
        </StyledMissionCard>
    )
}
