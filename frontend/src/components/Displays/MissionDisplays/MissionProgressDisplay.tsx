import { Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Task } from 'models/Task'
import { AttributeTitleTypography } from 'components/Styles/StyledComponents'

const StyledTagCount = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: space-between;
`
interface MissionProps {
    mission: Mission
}

export const MissionProgressDisplay = ({ mission }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const [completedTasks, setCompletedTasks] = useState<number>(0)

    const tasks = mission.tasks

    useEffect(() => {
        setCompletedTasks(countCompletedTasks(tasks))
    }, [tasks])

    const countCompletedTasks = (tasks: Task[]) => {
        return tasks.filter((task) => task.isCompleted).length
    }

    return (
        <StyledTagCount>
            <AttributeTitleTypography>{TranslateText('Completed Tasks')}</AttributeTitleTypography>
            <Typography>
                {completedTasks}/{tasks.length}
            </Typography>
        </StyledTagCount>
    )
}
