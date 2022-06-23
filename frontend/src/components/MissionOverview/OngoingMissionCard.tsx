import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from 'components/RobotCards/RobotStatusChip'
import { Mission } from 'models/mission'
import { defaultRobots } from 'models/robot'
import styled from 'styled-components'
const robots = [defaultRobots['taurob'], defaultRobots['exRobotics']]

interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 300px;
`
const HorisontalContent = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    padding-top: 2px;
`

export function OngoingMissionCard({ mission }: MissionProps) {
    return (
        <StyledMissionCard variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            <Typography variant="h6">INSPECTION</Typography>
            <Typography>Replace with name</Typography>
            <HorisontalContent>
                <Typography>Status section here</Typography>
                <Typography> Pause button here</Typography>
            </HorisontalContent>
        </StyledMissionCard>
    )
}
