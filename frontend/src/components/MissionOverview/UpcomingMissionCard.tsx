import { Button, Card, Checkbox, Icon, Typography } from '@equinor/eds-core-react'
import { more_vertical } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { format, differenceInHours } from 'date-fns'
import styled from 'styled-components'
import { useNavigate } from 'react-router-dom'
interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 700px;
    display: flex;
`
const HorizontalContent = styled.div`
    display: grid;
    grid-template-columns: 50px auto auto auto auto 50px;
    align-items: center;
`
const StyledMissionStartTime = styled.div`
    display: grid;
    grid-template-columns: auto auto auto;
    align-items: center;
`

Icon.add({ more_vertical })

export function UpcomingMissionCard({ mission }: MissionProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        let path = '/robotics-frontend/mission/' + mission.id
        navigate(path)
    }
    var numberOfTasks = 0
    mission.plannedTasks.forEach((task) => (numberOfTasks += task.inspections.length))
    return (
        <StyledMissionCard
            key={mission.id}
            variant="default"
            style={{ boxShadow: tokens.elevation.raised }}
            onClick={routeChange}
        >
            <HorizontalContent>
                <Checkbox />
                <Typography variant="h6">{mission.name}</Typography>
                <MissionStartTimeDisplay mission={mission} />
                <Typography>Tasks: {numberOfTasks}</Typography>
                <MissionDurationDisplay mission={mission} />
                <Button variant="ghost_icon">
                    <Icon name="more_vertical" size={24} title="more action" />
                </Button>
            </HorizontalContent>
        </StyledMissionCard>
    )
}

function MissionStartTimeDisplay({ mission }: MissionProps) {
    return (
        <StyledMissionStartTime>
            <Typography>Date: {format(new Date(mission.startTime), 'dd. MMM')}</Typography>
            <Typography>Time: {format(new Date(mission.startTime), 'hh:mm')}</Typography>
        </StyledMissionStartTime>
    )
}

function MissionDurationDisplay({ mission }: MissionProps) {
    if (mission.endTime) {
        return (
            <Typography>
                Estimated duration: {differenceInHours(new Date(mission.endTime), new Date(mission.startTime))} h
            </Typography>
        )
    }
    return <></>
}
