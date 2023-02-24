import { Typography } from '@equinor/eds-core-react'
import { IsarTask, IsarTaskStatus } from 'models/IsarTask'
import { Mission } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'

const StyledTagCount = styled.div`
    display: flex;
`
interface MissionProps {
    mission: Mission
}

export function MissionProgressDisplay({ mission }: MissionProps) {
    const tasks = mission.tasks
    const plannedTasks = mission.plannedTasks

    const [completedTasks, setCompletedTasks] = useState<number>(0)
    useEffect(() => {
        setCompletedTasks(countCompletedTasks(tasks))
    }, [tasks])

    const countCompletedTasks = (tasks: IsarTask[]) => {
        var counter = 0
        tasks.map((task) => (IsarTaskStatus.isComplete(task.taskStatus) ? (counter += 1) : (counter += 0)))
        return counter
    }

    return (
        <StyledTagCount>
            <Typography>
                {Text('Task')} {completedTasks}/{plannedTasks.length}
            </Typography>
        </StyledTagCount>
    )
}
