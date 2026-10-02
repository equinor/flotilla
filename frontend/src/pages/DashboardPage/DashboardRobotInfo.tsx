import { getRobotTypeString, RobotWithoutTelemetry, RobotStatus } from 'models/Robot'
import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { FieldLabel } from 'components/Styles/StyledComponents'
import { useRobotTelemetry } from 'hooks/useRobotTelemetry'
import { useLanguageContext } from 'contexts/LanguageContext'
import styled from 'styled-components'

const StyledRobotInfo = styled.div`
    display: flex;
    padding: 16px;
    align-items: center;
    gap: 16px;
    align-self: stretch;
    border-right: 1px solid ${tokens.colors.ui.background__medium.hex};
`
const StyledBody = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
`
const LongTypography = styled(Typography)`
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
`
const BigFieldLabel = styled(FieldLabel)`
    font-size: 0.85rem;
`
const HorizontalContent = styled.div`
    display: flex;
    align-items: flex-start;
    gap: 24px;
`
const VerticalContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`

interface DashboardRobotInfoProps {
    robot: RobotWithoutTelemetry
}

export const DashboardRobotInfo = ({ robot }: DashboardRobotInfoProps) => {
    const { TranslateText } = useLanguageContext()
    const { robotBatteryLevel, robotBatteryStatus, robotPressureLevel } = useRobotTelemetry(robot)

    return (
        <StyledRobotInfo>
            <RobotImage robotType={robot.type} height="96px" />
            <StyledBody>
                <LongTypography variant="h5">
                    {robot.name}
                    {` (${getRobotTypeString(robot.type)})`}
                </LongTypography>
                <HorizontalContent>
                    <VerticalContent>
                        <BigFieldLabel>{TranslateText('Status')}</BigFieldLabel>
                        <RobotStatusChip status={robot.status} itemSize={24} />
                    </VerticalContent>
                    {robot.status !== RobotStatus.Offline && (
                        <>
                            <VerticalContent>
                                <BigFieldLabel>{TranslateText('Battery')}</BigFieldLabel>
                                <BatteryStatusDisplay
                                    batteryLevel={robotBatteryLevel}
                                    batteryState={robotBatteryStatus}
                                    itemSize={24}
                                />
                            </VerticalContent>
                            {robotPressureLevel !== undefined && (
                                <VerticalContent>
                                    <BigFieldLabel>{TranslateText('Pressure')}</BigFieldLabel>
                                    <PressureStatusDisplay pressure={robotPressureLevel} itemSize={24} />
                                </VerticalContent>
                            )}
                        </>
                    )}
                </HorizontalContent>
            </StyledBody>
        </StyledRobotInfo>
    )
}
