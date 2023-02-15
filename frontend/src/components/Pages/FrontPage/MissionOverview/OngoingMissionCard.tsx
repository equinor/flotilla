import { Card, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { MissionProgressDisplay } from './MissionProgressDisplay'
import { MissionStatusDisplay } from './MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { MissionControlButtons } from './MissionControlButtons'

interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 300px;
    padding: 10px;
`
const HorisontalContent = styled.div`
    display: grid;
    grid-template-columns: auto auto 40px 40px;
    align-items: end;
`

export function OngoingMissionCard({ mission }: MissionProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }
    return (
        <StyledMissionCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <div onClick={routeChange}>
                <Typography variant="h6">{mission.name}</Typography>
                <Typography>{mission.robot.name}</Typography>
            </div>
            <HorisontalContent>
                <MissionStatusDisplay status={mission.missionStatus} />
                <MissionProgressDisplay mission={mission} />
                <MissionControlButtons mission={mission} />
            </HorisontalContent>
        </StyledMissionCard>
    )
}
