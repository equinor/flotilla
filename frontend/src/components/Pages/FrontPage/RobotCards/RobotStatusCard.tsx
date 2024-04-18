import { Card, Typography } from '@equinor/eds-core-react'
import { Robot, RobotStatus } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusChip'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import styled from 'styled-components'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { config } from 'config'
import { RobotType } from 'models/RobotModel'

interface RobotProps {
    robot: Robot
}

const StyledCard = styled(Card)`
    width: 220px;
    padding: 8px;
`
const HoverableStyledCard = styled(Card)`
    width: 220px;
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

const VerticalContent = styled.div<{ $alignItems?: string }>`
    display: flex;
    flex-direction: column;
    align-items: ${(props) => props.$alignItems};
    justify-content: flex-end;
    gap: 4px;
`

const StyledPadding = styled.div`
    padding: 8px;
`

const LongTypography = styled(Typography)`
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    :hover {
        overflow: visible;
        white-space: normal;
        text-overflow: unset;
        word-break: break-word;
    }
`

export const RobotStatusCard = ({ robot }: RobotProps) => {
    let navigate = useNavigate()
    const goToRobot = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/robot/${robot.id}`
        navigate(path)
    }
    return (
        <div>
            {robot != null && (
                <div>
                    <HoverableStyledCard style={{ boxShadow: tokens.elevation.raised }} onClick={goToRobot}>
                        <StyledPadding>
                            <RobotImage robotType={robot.model.type} height="200px" />
                            <LongTypography variant="h5">{robot.name}</LongTypography>
                            <HorizontalContent>
                                <VerticalContent $alignItems="start">
                                    <Typography variant="caption">{robot.model.type}</Typography>
                                    <RobotStatusChip status={robot.status} isarConnected={robot.isarConnected} />
                                </VerticalContent>
                                <VerticalContent $alignItems="end">
                                    {robot.status !== RobotStatus.Offline ? (
                                        <>
                                            {robot.pressureLevel && (
                                                <PressureStatusDisplay
                                                    pressure={robot.pressureLevel}
                                                    upperPressureWarningThreshold={
                                                        robot.model.upperPressureWarningThreshold
                                                    }
                                                    lowerPressureWarningThreshold={
                                                        robot.model.lowerPressureWarningThreshold
                                                    }
                                                />
                                            )}
                                            <BatteryStatusDisplay
                                                batteryLevel={robot.batteryLevel}
                                                batteryWarningLimit={robot.model.batteryWarningThreshold}
                                            />
                                        </>
                                    ) : (
                                        <></>
                                    )}
                                </VerticalContent>
                            </HorizontalContent>
                        </StyledPadding>
                    </HoverableStyledCard>
                </div>
            )}
        </div>
    )
}

export const RobotStatusCardPlaceholder = () => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledCard style={{ boxShadow: tokens.elevation.raised }}>
            <RobotImage robotType={RobotType.NoneType} />
            <Typography variant="h5" color="disabled">
                {TranslateText('No robot connected')}
            </Typography>
            <Typography variant="body_short" color="disabled">
                ----
            </Typography>
            <HorizontalContent>
                <RobotStatusChip isarConnected={true} />
            </HorizontalContent>
        </StyledCard>
    )
}
