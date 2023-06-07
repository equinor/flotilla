import { Card, Typography } from '@equinor/eds-core-react'
import { Robot } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from './RobotStatusChip'
import BatteryStatusView from './BatteryStatusView'
import styled from 'styled-components'
import { RobotImage } from './RobotImage'
import { useNavigate } from 'react-router-dom'
import { BatteryStatus } from 'models/Battery'
import { TranslateText } from 'components/Contexts/LanguageContext'
import PressureStatusView from './PressureStatusView'
import { config } from 'config'
import { RobotType } from 'models/RobotModel'

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

const HorizontalContent = styled.div`
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
            <RobotImage robotType={robot.model.type} />
            <HorizontalContent>
                <VerticalContent>
                    <Typography variant="h5">{robot.name}</Typography>
                    <Typography variant="caption">{robot.model.type}</Typography>
                    <RobotStatusChip status={robot.status} />
                </VerticalContent>
                <VerticalContent>
                    <PressureStatusView pressure={robot.pressureLevel} />
                    <BatteryStatusView battery={robot.batteryLevel} batteryStatus={BatteryStatus.Normal} />
                </VerticalContent>
            </HorizontalContent>
        </div>
    )
}

export function RobotStatusCard({ robot }: RobotProps) {
    let navigate = useNavigate()
    const goToRobot = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/robot/${robot.id}`
        navigate(path)
    }
    return (
        <HoverableStyledCard variant="default" style={{ boxShadow: tokens.elevation.raised }} onClick={goToRobot}>
            {cardContent({ robot })}
        </HoverableStyledCard>
    )
}

export function RobotStatusCardPlaceholder() {
    return (
        <StyledCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <div>
                <RobotImage robotType={RobotType.NoneType} />
                <Typography variant="h5" color="disabled">
                    {TranslateText('No robot connected')}
                </Typography>
                <Typography variant="body_short" color="disabled">
                    ----
                </Typography>
                <HorizontalContent>
                    <RobotStatusChip />
                    <BatteryStatusView />
                </HorizontalContent>
            </div>
        </StyledCard>
    )
}
