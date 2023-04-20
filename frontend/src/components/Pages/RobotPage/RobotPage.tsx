import { Typography } from '@equinor/eds-core-react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { BackButton } from '../MissionPage/MissionHeader/BackButton'
import { LocalizationSection } from './LocalizationSection'

const StyledRobotPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
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
            //.catch((e) => handleError(e))
        }
    }, [])

    return (
        <StyledRobotPage>
            <BackButton />
            <Typography variant="h1">{selectedRobot?.name + ' (' + selectedRobot?.model.type + ')'}</Typography>
            {selectedRobot !== undefined && <LocalizationSection robot={selectedRobot} />}
        </StyledRobotPage>
    )
}
