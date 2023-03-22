import { Card, Typography } from '@equinor/eds-core-react'
import { Robot, RobotStatus, RobotType } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from './RobotStatusChip'
import BatteryStatusView from './BatteryStatusView'
import styled from 'styled-components'
import { RobotImage } from './RobotImage'
import { useNavigate } from 'react-router-dom'
import { BatteryStatus } from 'models/Battery'
import { Text } from 'components/Contexts/LanguageContext'
import PressureStatusView from './PressureStatusView'

interface RobotProps {
    robot: Robot
}

export const card_width = 220

const StyledCard = styled(Card)`
    width: 220px;
    padding: 8px;
`
const HoverableStyledCard = styled(Card)`
    width: 220px;
    padding: 8px;
    :hover {
        background-color: #deedee;
    }
`

const HorisontalContent = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    padding-top: 2px;
`

const VerticalContent = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: flex-end;
`

function cardContent({ robot }: RobotProps) {
    return (
        <div>
            <RobotImage robotType={robot.model} />
            <HorisontalContent>
                <VerticalContent>
                    <Typography variant="h5">{robot.name}</Typography>
                    <Typography variant="caption">{RobotType.toString(robot.model)}</Typography>
                    <RobotStatusChip status={robot.status} />
                </VerticalContent>
                <VerticalContent>
                    <PressureStatusView pressure={robot.pressureLevel} />
                    <BatteryStatusView battery={robot.batteryLevel} batteryStatus={BatteryStatus.Normal} />
                </VerticalContent>
            </HorisontalContent>
        </div>
    )
}

export function RobotStatusCard({ robot }: RobotProps) {
    let navigate = useNavigate()
    const goToMission = () => {
        let path = `mission`
        navigate(path)
    }
    if (robot.status === RobotStatus.Busy) {
        return (
            <HoverableStyledCard variant="default" style={{ boxShadow: tokens.elevation.raised }} onClick={goToMission}>
                {cardContent({ robot })}
            </HoverableStyledCard>
        )
    }
    return (
        <StyledCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            {cardContent({ robot })}
        </StyledCard>
    )
}

export function RobotStatusCardPlaceholder() {
    return (
        <StyledCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <div>
                <RobotImage robotType={RobotType.NoneType} />
                <Typography variant="h5" color="disabled">
                    {Text('No robot connected')}
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
