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

const StyledTitle = styled(Card)`
    width: 70%;
    height: 80%;
    padding: 5px;
    :hover {
        background-color: #deedee;
    }
`
const HorizontalContent = styled.div`
    display: grid;
    grid-template-columns: auto auto auto;
    align-items: end;
    gap: 1rem;
`
const TopContent = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
`
const BottomContent = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    align-items: end;
    padding-right: 25px;
`
const VerticalContent = styled.div`
    display: grid;
    grid-template-rows: auto auto;
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
                <StyledTitle variant="default" onClick={routeChange}>
                    <Typography variant="h6" color="primary">
                        {mission.name}
                    </Typography>
                </StyledTitle>
                <div>
                    <MissionControlButtons mission={mission} />
                </div>
            </TopContent>
            <BottomContent>
                <VerticalContent>
                    <Typography variant="meta" color="#6F6F6F">
                        {'Status'}
                    </Typography>
                    <HorizontalContent>
                        <MissionStatusDisplay status={mission.status} />
                        <MissionProgressDisplay mission={mission} />
                        <BatteryStatusView battery={mission.robot.batteryLevel} batteryStatus={BatteryStatus.Normal} />
                    </HorizontalContent>
                </VerticalContent>
                <div>
                    <Typography variant="meta" color="#6F6F6F">
                        {'Robot'}
                    </Typography>
                    <Typography variant="body_short" color="#3D3D3D">
                        {' '}
                        {mission.robot.name}
                    </Typography>
                </div>
            </BottomContent>
        </StyledMissionCard>
    )
}
