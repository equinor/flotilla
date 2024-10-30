import { Card, Typography } from '@equinor/eds-core-react'
import { Robot, RobotStatus } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import styled from 'styled-components'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { config } from 'config'
import { RobotType } from 'models/RobotModel'
import { Mission } from 'models/Mission'

interface RobotProps {
    robot: Robot
    mission: Mission | undefined
}

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

    #buttoncard:hover {
        background-color: ${tokens.colors.infographic.primary__mist_blue.hex};
    }

    :hover + #buttoncard {
        background-color: ${tokens.colors.infographic.primary__mist_blue.hex};
    }
`
const ButtonCard = styled.div`
    background-color: ${tokens.colors.ui.background__light.hex};
    padding: 10px;
    border-radius: 0px 0px 6px 6px;
    height: 100%;
`
const HorizontalContent = styled.div`
    display: flex;
    flex-direction: row;
    align-content: start;
    align-items: start;
    justify-content: space-between;
    gap: 4px;
    padding-top: 2px;
`
const VerticalContent = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: left;
    gap: 4px;
`
const AreaContent = styled.div`
    display: flex;
    justify-content: left;
    padding-right: 6px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
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

export const RobotStatusCard = ({ robot, mission }: RobotProps) => {
    let navigate = useNavigate()
    const { TranslateText } = useLanguageContext()
    const goToRobot = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/robot/${robot.id}`
        navigate(path)
    }

    const getRobotModel = (type: RobotType) => {
        if (type === RobotType.TaurobInspector || type === RobotType.TaurobOperator) return 'Taurob'
        return type
    }

    return (
        <HoverableStyledCard style={{ boxShadow: tokens.elevation.raised }} onClick={goToRobot}>
            <RobotImage robotType={robot.model.type} height="180px" />
            <ButtonCard id="buttoncard">
                <HorizontalContent>
                    <LongTypography variant="h5">
                        {robot.name}
                        {' ('}
                        {getRobotModel(robot.model.type)}
                        {')'}
                    </LongTypography>
                    {mission?.area?.areaName && (
                        <AreaContent>
                            <Typography variant="h5"> {mission.area.areaName} </Typography>
                        </AreaContent>
                    )}
                </HorizontalContent>
                <HorizontalContent>
                    <VerticalContent>
                        <Typography variant="meta">{TranslateText('Status')}</Typography>
                        <RobotStatusChip
                            status={robot.status}
                            flotillaStatus={robot.flotillaStatus}
                            isarConnected={robot.isarConnected}
                        />
                    </VerticalContent>

                    {robot.status !== RobotStatus.Offline ? (
                        <>
                            <VerticalContent>
                                <Typography variant="meta">{TranslateText('Battery')}</Typography>
                                <BatteryStatusDisplay
                                    batteryLevel={robot.batteryLevel}
                                    batteryWarningLimit={robot.model.batteryWarningThreshold}
                                />
                            </VerticalContent>

                            {robot.pressureLevel !== undefined && robot.pressureLevel !== null && (
                                <VerticalContent>
                                    <Typography variant="meta">{TranslateText('Pressure')}</Typography>
                                    <PressureStatusDisplay
                                        pressure={robot.pressureLevel}
                                        upperPressureWarningThreshold={robot.model.upperPressureWarningThreshold}
                                        lowerPressureWarningThreshold={robot.model.lowerPressureWarningThreshold}
                                    />
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
                <RobotStatusChip isarConnected={true} />
            </HorizontalContent>
        </StyledCard>
    )
}
