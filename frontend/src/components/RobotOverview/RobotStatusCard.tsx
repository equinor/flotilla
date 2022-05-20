import { Card, Typography } from '@equinor/eds-core-react'
import { Robot } from 'models/robot'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from './RobotStatusChip'
import { RobotImage } from './RobotImage'
import BatteryStatusView from './BatteryStatusView'

interface RobotProps {
    robot: Robot
}

const card_width = 200

export function RobotStatusCard({ robot }: RobotProps) {
    return (
        <Card
            className={'robotStatusCard' + robot.robotInfo.name}
            variant="default"
            style={{ width: card_width, boxShadow: tokens.elevation.sticky }}
        >
            <Card.Media className={'robotImage' + robot.robotInfo.type} fullWidth={true}>
                <RobotImage robotType={robot.robotInfo.type} />
            </Card.Media>
            <Card.Header>
                <Card.HeaderTitle>
                    <Typography className="robotName" variant="h5">
                        {robot.robotInfo.name}
                    </Typography>
                    <Typography className="robotType" variant="body_short">
                        {robot.robotInfo.type}
                    </Typography>
                </Card.HeaderTitle>
            </Card.Header>
            <Card.Content>
                <RobotStatusChip status={robot.status} />
                <BatteryStatusView battery={robot.battery} />
            </Card.Content>
        </Card>
    )
}
