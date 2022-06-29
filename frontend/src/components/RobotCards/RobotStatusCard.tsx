import { Card, Typography } from '@equinor/eds-core-react'
import { Robot, RobotStatus } from 'models/robot'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from './RobotStatusChip'
import BatteryStatusView from './BatteryStatusView'
import styled from 'styled-components'
import { RobotImage } from './RobotImage'
import { Link, useHref, useNavigate } from 'react-router-dom'
import { Mission } from 'models/mission'

interface RobotProps {
    robot: Robot
}

export const card_width = 200

const StyledCard = styled(Card)`
    width: 200px;
    padding: 8px;
`
const HoverableStyledCard = styled(Card)`
    width: 200px;
    padding: 8px;
    :hover{
        background-color: #DEEDEE;
    }
`

const HorisontalContent = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    padding-top: 2px;
`

function cardContent({ robot }: RobotProps) {
    return (
        <div>
            <RobotImage robotType={robot.robotInfo.type} />
            <Typography variant="h5">{robot.robotInfo.name}</Typography>
            <Typography variant="body_short">{robot.robotInfo.type}</Typography>
            <HorisontalContent>
                <RobotStatusChip status={robot.status} />
                <BatteryStatusView battery={robot.battery} />
            </HorisontalContent>
        </div>
    )
}

export function RobotStatusCard({ robot }: RobotProps) {
    let navigate = useNavigate(); 
    const goToMission = () => { 
        let path = `mission`;
        navigate(path);
    }
    if (robot.status == RobotStatus.MissionInProgress) {
        return (
            <HoverableStyledCard variant="default" style={{ boxShadow: tokens.elevation.sticky }} onClick={goToMission}>
                {cardContent({robot})}
            </HoverableStyledCard>
        )
    }
    return (
        <StyledCard variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            {cardContent({robot})}
        </StyledCard>
    )
}

export function RobotStatusCardPlaceholder() {
    return (
        <StyledCard variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            <div>
                <RobotImage />
                <Typography variant="h5" color="disabled">
                    No robot connected
                </Typography>
                <Typography variant="body_short" color="disabled">
                    ----
                </Typography>
                <HorisontalContent>
                    <RobotStatusChip />
                    <BatteryStatusView />
                </HorisontalContent>
            </div>
        </StyledCard>
    )
}
