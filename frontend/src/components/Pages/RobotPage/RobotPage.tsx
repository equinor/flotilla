import { Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { BackButton } from '../../../utils/BackButton'
import { LocalizationSection } from './LocalizationSection'
import { Header } from 'components/Header/Header'
import { RobotImage } from '../FrontPage/RobotCards/RobotImage'
import { MoveRobotArm } from './RobotArmMovement'
import PressureStatusView from '../FrontPage/RobotCards/PressureStatusView'
import BatteryStatusView from '../FrontPage/RobotCards/BatteryStatusView'
import { BatteryStatus } from 'models/Battery'
import { RobotStatusChip } from '../FrontPage/RobotCards/RobotStatusChip'
import { RobotStatus } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotType } from 'models/RobotModel'

const StyledRobotPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: flex-start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
`

const StyledButtons = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
`
const RobotInfo = styled.div`
    display: flex;
    align-items: start;
    gap: 1rem;
`
const VerticalContent = styled.div<{ $alignItems?: string }>`
    display: flex;
    flex-direction: column;
    align-items: ${(props) => props.$alignItems};
    justify-content: flex-end;
    gap: 2rem;
`

const updateSiteTimer = 1000
export function RobotPage() {
    const { TranslateText } = useLanguageContext()
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const { robotId } = useParams()

    useEffect(() => {
        fetchRobotData()
    }, [robotId])

    useEffect(() => {
        const intervalId = setInterval(fetchRobotData, updateSiteTimer)
        return () => {
            clearInterval(intervalId)
        }
    }, [])

    const fetchRobotData = () => {
        if (robotId) {
            BackendAPICaller.getRobotById(robotId).then((robot) => {
                setSelectedRobot(robot)
            })
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
                            <VerticalContent $alignItems="start">
                                {selectedRobot.status !== RobotStatus.Offline && (
                                    <>
                                        <BatteryStatusView
                                            itemSize={48}
                                            battery={selectedRobot.batteryLevel}
                                            batteryStatus={BatteryStatus.Normal}
                                            robotStatus={selectedRobot.status}
                                        />
                                        {selectedRobot.model.upperPressureWarningThreshold && (
                                            <PressureStatusView
                                                itemSize={48}
                                                pressureInBar={selectedRobot.pressureLevel}
                                                upperPressureWarningThreshold={
                                                    selectedRobot.model.upperPressureWarningThreshold
                                                }
                                                lowerPressureWarningThreshold={
                                                    selectedRobot.model.lowerPressureWarningThreshold
                                                }
                                                robotStatus={selectedRobot.status}
                                            />
                                        )}
                                    </>
                                )}
                                <RobotStatusChip status={selectedRobot.status} />
                            </VerticalContent>
                        </RobotInfo>

                        <LocalizationSection robot={selectedRobot} />
                        {selectedRobot.model.type === RobotType.TaurobInspector && (
                            <>
                                <Typography variant="h2">{TranslateText('Move robot arm')}</Typography>
                                <StyledButtons>
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
                                </StyledButtons>
                            </>
                        )}
                    </>
                )}
            </StyledRobotPage>
        </>
    )
}
