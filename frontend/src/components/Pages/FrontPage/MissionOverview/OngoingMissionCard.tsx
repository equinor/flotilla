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

interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 432px;
    padding: 10px;
`
const HorisontalContent = styled.div`
    display: grid;
    grid-template-columns: auto auto auto auto;
    align-items: end;
`
const TopContent = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
`

export function OngoingMissionCard({ mission }: MissionProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }
    return (
        <StyledMissionCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <TopContent>
            <div onClick={routeChange}>
                <Typography variant="h6">{mission.name}</Typography>
                <Typography>{mission.robot.name}</Typography>
            </div>
            <div>
            <MissionControlButtons mission={mission} />
            </div>
            </TopContent>
            <HorisontalContent>
                <MissionStatusDisplay status={mission.status} />
                <MissionProgressDisplay mission={mission} />
                <BatteryStatusView battery={mission.robot.batteryLevel} batteryStatus={BatteryStatus.Normal} />
                
            </HorisontalContent>
        </StyledMissionCard>
    )
}
