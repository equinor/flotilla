import { getRobotTypeString, RobotWithoutTelemetry, RobotStatus, RobotType } from 'models/Robot'
import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import styled from 'styled-components'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { config } from 'config'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { useRobotTelemetry } from 'hooks/useRobotTelemetry'

const StyledCard = styled(Card)`
    width: 220px;
    padding: 12px;
`
const HoverableStyledCard = styled(Card)`
    display: flex;
    flex-direction: column;
    width: 280px;
    gap: 0px;
    background-color: ${tokens.colors.ui.background__default.hex};
    cursor: pointer;
    :hover + #bottomcard {
        background-color: ${tokens.colors.infographic.primary__mist_blue.hex};
    }
`

const ButtonCard = styled.div`
    background-color: ${tokens.colors.ui.background__light.hex};
    padding: 10px;
    border-radius: 0px 0px 6px 6px;
    pointer-events: auto;
`
const HorizontalContent = styled.div`
    display: flex;
    flex-direction: row;
    align-content: end;
    align-items: end;
    justify-content: space-between;
    padding-top: 2px;
`
const VerticalContent = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: left;
    gap: 4px;
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

interface RobotStatusCardProps {
    robot: RobotWithoutTelemetry
}

export const RobotStatusCard = ({ robot }: RobotStatusCardProps) => {
    const navigate = useNavigate()
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useAssetContext()
    const { robotBatteryLevel, robotBatteryStatus, robotPressureLevel } = useRobotTelemetry(robot)

    const goToRobot = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/${installationCode}:robot?id=${robot.id}`
        navigate(path)
    }

    return (
        <HoverableStyledCard style={{ boxShadow: tokens.elevation.raised }} onClick={goToRobot}>
            <RobotImage robotType={robot.type} height="180px" />
            <ButtonCard id="bottomcard">
                <LongTypography variant="h5">
                    {robot.name}
                    {` (${getRobotTypeString(robot.type)})`}
                </LongTypography>
                <HorizontalContent>
                    <VerticalContent>
                        <Typography variant="meta">{TranslateText('Status')}</Typography>
                        <RobotStatusChip status={robot.status} />
                    </VerticalContent>

                    {robot.status !== RobotStatus.Offline ? (
                        <>
                            <VerticalContent>
                                <Typography variant="meta">{TranslateText('Battery')}</Typography>
                                <BatteryStatusDisplay
                                    batteryLevel={robotBatteryLevel}
                                    batteryState={robotBatteryStatus}
                                />
                            </VerticalContent>

                            {robotPressureLevel !== undefined && (
                                <VerticalContent>
                                    <Typography variant="meta">{TranslateText('Pressure')}</Typography>
                                    <PressureStatusDisplay pressure={robotPressureLevel} />
                                </VerticalContent>
                            )}
                        </>
                    ) : (
                        <></>
                    )}
                </HorizontalContent>
            </ButtonCard>
        </HoverableStyledCard>
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
            <HorizontalContent>
                <RobotStatusChip />
            </HorizontalContent>
        </StyledCard>
    )
}
