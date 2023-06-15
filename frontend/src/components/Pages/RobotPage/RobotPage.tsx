import { Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { BackButton } from '../MissionPage/MissionHeader/BackButton'
import { LocalizationSection } from './LocalizationSection'
import { Header } from 'components/Header/Header'
import { RobotImage } from '../FrontPage/RobotCards/RobotImage'
import { MoveRobotArm } from './RobotArmMovement'
import PressureStatusView from '../FrontPage/RobotCards/PressureStatusView'
import BatteryStatusView from '../FrontPage/RobotCards/BatteryStatusView'
import { BatteryStatus } from 'models/Battery'
import { RobotStatusChip } from '../FrontPage/RobotCards/RobotStatusChip'
import { RobotStatus } from 'models/Robot'
import { RobotType } from 'models/RobotModel'
import { TranslateText } from 'components/Contexts/LanguageContext'

const StyledRobotPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
`

const StyledButtons = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
`

export function RobotPage() {
    const { robotId } = useParams()
    const [selectedRobot, setSelectedRobot] = useState<Robot>()

    useEffect(() => {
        if (robotId) {
            BackendAPICaller.getRobotById(robotId).then((robot) => {
                setSelectedRobot(robot)
            })
        }
    }, [robotId])
    return (
        <>
            <Header page={'robot'} />
            <StyledRobotPage>
                <BackButton />
                <Typography variant="h1">{selectedRobot?.name + ' (' + selectedRobot?.model.type + ')'}</Typography>
                <RobotImage robotType={selectedRobot?.model.type} />
                {selectedRobot && (
                    <>
                        <BatteryStatusView battery={selectedRobot.batteryLevel} batteryStatus={BatteryStatus.Normal} />
                        <PressureStatusView pressure={selectedRobot.pressureLevel} />
                        <RobotStatusChip status={selectedRobot.status} />
                        <LocalizationSection robot={selectedRobot} />
                        {selectedRobot.status === RobotStatus.Available &&
                            selectedRobot.model.type === RobotType.TaurobInspector && (
                                <>
                                    <Typography variant="h2">{TranslateText('Move robot arm')}</Typography>
                                    <StyledButtons>
                                        <MoveRobotArm robot={selectedRobot} armPosition="battery_change" />
                                        <MoveRobotArm robot={selectedRobot} armPosition="transport" />
                                        <MoveRobotArm robot={selectedRobot} armPosition="lookout" />
                                    </StyledButtons>
                                </>
                            )}
                    </>
                )}
            </StyledRobotPage>
        </>
    )
}
