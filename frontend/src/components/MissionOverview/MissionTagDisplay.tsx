import { Typography } from '@equinor/eds-core-react'
import { IsarTask, IsarTaskStatus } from 'models/IsarTask'
import styled from 'styled-components'

const StyledTagCount = styled.div`
    display: flex;
`
interface TaskProps {
    tasks: IsarTask[]
}

export function MissionProgressDisplay({ tasks }: TaskProps) {
    var numberOfTasks = tasks.length
    var numberOfCompletedTasks = 0
    tasks.forEach((task) => {
        if (task.taskStatus == IsarTaskStatus.Successful) numberOfCompletedTasks++
    })
    return (
        <StyledTagCount>
            <Typography>
                Tag {numberOfCompletedTasks}/{numberOfTasks}
            </Typography>
        </StyledTagCount>
    )
}
