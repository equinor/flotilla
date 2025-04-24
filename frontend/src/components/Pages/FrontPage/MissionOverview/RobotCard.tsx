import { Button, Icon, Typography } from '@equinor/eds-core-react'
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
import { StyledButton, AttributeTitleTypography } from 'components/Styles/StyledComponents'
import { Icons } from 'utils/icons'

const StyledRobotCard = styled.div`
    display: flex;
    align-self: stretch;

    @media (min-width: 960px) {
        width: 446px;
        padding: 16px;
        align-items: center;
        gap: 16px;
        border-right: 1px solid ${tokens.colors.ui.background__medium.hex};
    }

    @media (max-width: 960px) {
        padding: 8px;
        flex-direction: column;
        justify-content: center;
        align-items: flex-start;
        gap: 8px;
        border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
    }
`
const HorizontalContent = styled.div`
    display: flex;
    align-items: flex-start;
    gap: 24px;
    align-self: stretch;
`
const VerticalContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`
const StyledNoneImageBody = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    align-self: stretch;
    justify-content: space-between;

    @media (min-width: 960px) {
        gap: 16px;
    }
`
const StyledMainBody = styled.div`
    align-self: stretch;

    @media (min-width: 960px) {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        gap: 8px;
    }
`
const StyledHeader = styled.div`
    display: flex;
    flex-direction: row;
    align-self: stretch;
    justify-content: space-between;
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
const StyledGhostButton = styled(StyledButton)`
    padding: 0;
`
const HiddenOnSmallScreen = styled.div`
    @media (max-width: 960px) {
        display: none;
    }
`
const HiddenOnLargeScreen = styled.div`
    @media (min-width: 960px) {
        display: none;
    }
`

export const RobotCard = ({ robot }: { robot: Robot }) => {
    const navigate = useNavigate()
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
        <StyledRobotCard>
            <HiddenOnSmallScreen>
                <RobotImage robotType={robot.model.type} height="88px" />
            </HiddenOnSmallScreen>
            <StyledNoneImageBody>
                <StyledMainBody>
                    <StyledHeader onClick={goToRobot}>
                        <LongTypography variant="h5">
                            {robot.name}
                            {' ('}
                            {getRobotModel(robot.model.type)}
                            {')'}
                        </LongTypography>
                        <HiddenOnLargeScreen>
                            <Button variant="ghost_icon">
                                <Icon name={Icons.RightCheveron} size={24} />
                            </Button>
                        </HiddenOnLargeScreen>
                    </StyledHeader>
                    <HorizontalContent>
                        <VerticalContent>
                            <AttributeTitleTypography>{TranslateText('Status')}</AttributeTitleTypography>
                            <RobotStatusChip
                                status={robot.status}
                                flotillaStatus={robot.flotillaStatus}
                                isarConnected={robot.isarConnected}
                            />
                        </VerticalContent>

                        {robot.status !== RobotStatus.Offline ? (
                            <>
                                <VerticalContent>
                                    <AttributeTitleTypography>{TranslateText('Battery')}</AttributeTitleTypography>
                                    <BatteryStatusDisplay
                                        batteryLevel={robot.batteryLevel}
                                        batteryState={robot.batteryState}
                                        batteryWarningLimit={robot.model.batteryWarningThreshold}
                                    />
                                </VerticalContent>

                                {robot.pressureLevel !== undefined && robot.pressureLevel !== null && (
                                    <VerticalContent>
                                        <AttributeTitleTypography>{TranslateText('Pressure')}</AttributeTitleTypography>
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
                </StyledMainBody>
                <HiddenOnSmallScreen>
                    <StyledGhostButton variant="ghost" onClick={goToRobot}>
                        {TranslateText('Open robot information')}
                        <Icon name={Icons.RightCheveron} size={16} />
                    </StyledGhostButton>
                </HiddenOnSmallScreen>
            </StyledNoneImageBody>
        </StyledRobotCard>
    )
}

export const RobotCardPlaceholder = () => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledRobotCard>
            <RobotImage robotType={RobotType.NoneType} height="88px" />
            <StyledNoneImageBody>
                <Typography variant="h5" color="disabled">
                    {TranslateText('No robot connected')}
                </Typography>
                <RobotStatusChip isarConnected={true} />
            </StyledNoneImageBody>
        </StyledRobotCard>
    )
}
