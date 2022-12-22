import { Typography } from '@equinor/eds-core-react'
import { IsarTask, IsarTaskStatus } from 'models/IsarTask'
import { useEffect, useState } from 'react'
import styled from 'styled-components'

const StyledTagCount = styled.div`
    display: flex;
`
interface TaskProps {
    tasks: IsarTask[]
}

interface TaskProps {
    tasks: IsarTask[]
}

export function MissionProgressDisplay({ tasks }: TaskProps) {
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
                Task {completedTasks}/{tasks.length}
            </Typography>
        </StyledTagCount>
    )
}
