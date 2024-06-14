import { Typography, Icon } from '@equinor/eds-core-react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { BackButton } from 'utils/BackButton'
import { Header } from 'components/Header/Header'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { MoveRobotArm } from './RobotArmMovement'
import { PressureTable } from './PressureTable'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusIcon'
import { RobotStatus } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotType } from 'models/RobotModel'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { StyledButton, StyledPage } from 'components/Styles/StyledComponents'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'


const ActionSection = styled.div`
    display: flex;
    flex-direction: column;    
    gap: 1rem;
`
const RobotArmMovementSection = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
    @media (max-width: 600px) {
        flex-direction: column;
    }
`
const DocumentSection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;

`
const Info = styled.div`
    display: flex;
    flex-wrap: wrap;
    flex-direction: row;
    gap: 7rem;
`
const DocumentRow = styled.div`
    display: flex;
    flex-direction: row;
    gap: 2rem;
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
    @media (max-width: 600px) {
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
    @media (max-width: 600px) {
        flex-direction: row;
        align-items: flex-end;
    }
`

export const RobotPage = () => {
    const { TranslateText } = useLanguageContext()
    const { setAlert } = useAlertContext()
    const { robotId } = useParams()
    const { enabledRobots } = useRobotContext()

    const selectedRobot = enabledRobots.find((robot) => robot.id === robotId)

    const returnRobotToHome = () => {
        if (robotId) {
            BackendAPICaller.returnRobotToHome(robotId).catch((e) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent
                        translatedMessage={TranslateText('Failed to send robot {0} home', [selectedRobot?.name ?? ''])}
                    />,
                    AlertCategory.ERROR
                )
            })
        }
    }

    return (
        <>
            <Header page={'robot'} />
            <StyledPage>
                <BackButton />
                {selectedRobot && (
                    <>
                        <Typography variant="h1">
                            {selectedRobot.name + ' (' + selectedRobot.model.type + ')'}
                        </Typography>

                        <div>
                            <RobotInfo>
                                <RobotImage height="350px" robotType={selectedRobot.model.type} />
                                <StatusContent $alignItems="start">
                                    <RobotStatusChip
                                        status={selectedRobot.status}
                                    flotillaStatus={selectedRobot.flotillaStatus}
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
                        </div>
                        <Info>
                            <div>
                                {selectedRobot.model.type === RobotType.TaurobInspector && <PressureTable />}
                            </div>

                            <ActionSection>
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
                            </ActionSection>
                            
                            <DocumentSection>
                            <Typography variant="h2">{TranslateText('STID Documents')}</Typography>
                            {selectedRobot.model.type === RobotType.TaurobInspector && (
                                <>
                                    <DocumentRow>
                                        <Icon name={Icons.FileDescription} color={tokens.colors.interactive.primary__resting.hex} size={24}  />
                                        <Typography variant="h4" color={tokens.colors.interactive.primary__resting.hex}>
                                            <a href="https://stid.equinor.com/JSV/file/8ac6f817-97d6-4ff9-bab7-d84437c8637d.pdf?docNo=C151-EQ-J-MB-00001&revNo=01">
                                                {TranslateText('Drifts of vedlikeholdsmanual')}
                                            </a>                                    
                                        </Typography>
                                    </DocumentRow>
                                    <DocumentRow>
                                        <Icon name={Icons.FileDescription} color={tokens.colors.interactive.primary__resting.hex} size={24}  />
                                        <Typography variant="h4" color={tokens.colors.interactive.primary__resting.hex}>
                                            <a href="https://stid.equinor.com/JSV/file/32144945-731f-4ee6-93d9-ba3fbeefe06c.pdf?docNo=C151-EQ-J-MB-00002&revNo=01">
                                                {TranslateText('Roles and responsibilities')}
                                            </a>                                    
                                        </Typography>
                                    </DocumentRow>
                                </>
                            )}
                            </DocumentSection>
                        </Info>
                    </>
                )}
            </StyledPage>
        </>
    )
}
