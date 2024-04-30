import { Typography } from '@equinor/eds-core-react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { MoveRobotArm } from './RobotArmMovement'
import { PressureTable } from './PressureTable'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusChip'
import { RobotStatus } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotType } from 'models/RobotModel'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { StyledButton } from 'components/Styles/StyledComponents'

const StyledRobotPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: flex-start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
`
const RobotArmMovementSection = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
    @media (max-width: 800px) {
        flex-direction: column;
    }
`
const StyledTextButton = styled(StyledButton)`
    text-align: left;
    max-width: 12rem;
`
const RobotInfo = styled.div`
    display: flex;
    flex-direction: row;
    align-items: flex-start;
    gap: 3rem;
    width: calc(80vw);
    @media (max-width: 800px) {
        flex-direction: column;
    }
    margin: 0rem 0rem 2rem 0rem;
`
const StatusContent = styled.div<{ $alignItems?: string }>`
    display: flex;
    flex-direction: column;
    align-items: ${(props) => props.$alignItems};
    justify-content: flex-end;
    gap: 2rem;
    @media (max-width: 800px) {
        flex-direction: row;
        align-items: flex-end;
    }
`

export const RobotPage = () => {
    const { TranslateText } = useLanguageContext()
    const { robotId } = useParams()
    const { enabledRobots } = useRobotContext()

    const selectedRobot = enabledRobots.find((robot) => robot.id === robotId)

    const returnRobotToHome = () => {
        if (robotId) {
            BackendAPICaller.returnRobotToHome(robotId)
        }
    }

    return (
        <>
            <Header page={'robot'} />
            <StyledRobotPage>
                <BackButton />
                {selectedRobot && (
                    <>
                        <Typography variant="h1">
                            {selectedRobot.name + ' (' + selectedRobot.model.type + ')'}
                        </Typography>
                        <RobotInfo>
                            <RobotImage height="350px" robotType={selectedRobot.model.type} />
                            <StatusContent $alignItems="start">
                                <RobotStatusChip
                                    status={selectedRobot.status}
                                    isarConnected={selectedRobot.isarConnected}
                                />

                                {selectedRobot.status !== RobotStatus.Offline && (
                                    <>
                                        <BatteryStatusDisplay
                                            itemSize={48}
                                            batteryLevel={selectedRobot.batteryLevel}
                                            batteryWarningLimit={selectedRobot.model.batteryWarningThreshold}
                                        />
                                        {selectedRobot.pressureLevel !== null &&
                                            selectedRobot.pressureLevel !== undefined && (
                                                <PressureStatusDisplay
                                                    itemSize={48}
                                                    pressure={selectedRobot.pressureLevel}
                                                    upperPressureWarningThreshold={
                                                        selectedRobot.model.upperPressureWarningThreshold
                                                    }
                                                    lowerPressureWarningThreshold={
                                                        selectedRobot.model.lowerPressureWarningThreshold
                                                    }
                                                />
                                            )}
                                    </>
                                )}
                            </StatusContent>
                        </RobotInfo>
                        {selectedRobot.model.type === RobotType.TaurobInspector && <PressureTable />}
                        <Typography variant="h2">{TranslateText('Actions')}</Typography>

                        <StyledTextButton variant="outlined" onClick={returnRobotToHome}>
                            {TranslateText('Return robot to home')}
                        </StyledTextButton>

                        {selectedRobot.model.type === RobotType.TaurobInspector && (
                            <>
                                <Typography variant="h4">{TranslateText('Set robot arm to ')}</Typography>
                                <RobotArmMovementSection>
                                    <MoveRobotArm
                                        robot={selectedRobot}
                                        armPosition="battery_change"
                                        isRobotAvailable={selectedRobot.status === RobotStatus.Available}
                                    />
                                    <MoveRobotArm
                                        robot={selectedRobot}
                                        armPosition="transport"
                                        isRobotAvailable={selectedRobot.status === RobotStatus.Available}
                                    />
                                    <MoveRobotArm
                                        robot={selectedRobot}
                                        armPosition="lookout"
                                        isRobotAvailable={selectedRobot.status === RobotStatus.Available}
                                    />
                                </RobotArmMovementSection>
                            </>
                        )}
                    </>
                )}
            </StyledRobotPage>
        </>
    )
}
