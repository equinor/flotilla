import { Card, Typography } from '@equinor/eds-core-react'
import { Robot } from 'models/robot'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from './RobotStatusChip'
import BatteryStatusView from './BatteryStatusView'
import styled from 'styled-components'
import { RobotImage } from './RobotImage'

interface RobotProps {
    robot: Robot
}

export const card_width = 200

const StyledCard = styled(Card)`
    width: 200px;
    padding: 8px;
`

const HorisontalContent = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    padding-top: 2px;
`

export function RobotStatusCard({ robot }: RobotProps) {
    return (
        <StyledCard variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            <div>
                <RobotImage robotType={robot.robotInfo.type} />
                <Typography variant="h5">{robot.robotInfo.name}</Typography>
                <Typography variant="body_short">{robot.robotInfo.type}</Typography>
                <HorisontalContent>
                    <RobotStatusChip status={robot.status} />
                    <BatteryStatusView battery={robot.battery} />
                </HorisontalContent>
            </div>
        </StyledCard>
    )
}
