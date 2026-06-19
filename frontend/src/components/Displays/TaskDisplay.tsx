import { Typography } from '@equinor/eds-core-react'
import { MissionTaskDefinition } from 'models/MissionDefinition'
import { Task } from 'models/Task'

export const TagIdDisplay = ({ task }: { task: Task | MissionTaskDefinition }) => {
    if (!task.tagId) return <Typography key={task.id + 'tagId'}>{'N/A'}</Typography>
    else return <Typography key={task.id + 'tagId'}>{task.tagId!}</Typography>
}

export const DescriptionDisplay = ({ task }: { task: Task | MissionTaskDefinition }) => {
    if (!task.description) return <Typography key={task.id + 'descr'}>{'N/A'}</Typography>
    return <Typography key={task.id + 'descr'}>{task.description}</Typography>
}
