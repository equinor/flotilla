import { Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { translateText } from 'components/Contexts/LanguageContext'
import { Task } from 'models/Task'

const StyledTagCount = styled.div`
    display: flex;
`
interface MissionProps {
    mission: Mission
}

export function MissionProgressDisplay({ mission }: MissionProps) {
    const tasks = mission.tasks

    const [completedTasks, setCompletedTasks] = useState<number>(0)
    useEffect(() => {
        setCompletedTasks(countCompletedTasks(tasks))
    }, [tasks])

    const countCompletedTasks = (tasks: Task[]) => {
        return tasks.filter((task) => task.isCompleted).length
    }

    return (
        <StyledTagCount>
            <Typography>
                {translateText('Task')} {completedTasks}/{tasks.length}
            </Typography>
        </StyledTagCount>
    )
}
