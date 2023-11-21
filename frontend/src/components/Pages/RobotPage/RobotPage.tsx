import { Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { BackButton } from 'utils/BackButton'
import { LocalizationSection } from './LocalizationSection'
import { Header } from 'components/Header/Header'
import { RobotImage } from 'components/Displays/RobotDisplays/RobotImage'
import { MoveRobotArm } from './RobotArmMovement'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { BatteryStatusDisplay } from 'components/Displays/RobotDisplays/BatteryStatusDisplay'
import { RobotStatusChip } from 'components/Displays/RobotDisplays/RobotStatusChip'
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
    const { robotId } = useParams()
    const [selectedRobot, setSelectedRobot] = useState<Robot>()

    const fetchRobotData = useCallback(() => {
        if (robotId) {
            BackendAPICaller.getRobotById(robotId).then((robot) => {
                setSelectedRobot(robot)
            })
        }
    }, [robotId])

    useEffect(() => {
        fetchRobotData()
    }, [fetchRobotData])

    useEffect(() => {
        const intervalId = setInterval(fetchRobotData, updateSiteTimer)
        return () => {
            clearInterval(intervalId)
        }
    }, [fetchRobotData])

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
                                        <BatteryStatusDisplay itemSize={48} batteryLevel={selectedRobot.batteryLevel} />
                                        {selectedRobot.model.upperPressureWarningThreshold && (
                                            <PressureStatusDisplay
                                                itemSize={48}
                                                pressureInBar={selectedRobot.pressureLevel}
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
