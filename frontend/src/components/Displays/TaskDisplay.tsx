import { Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'

export const TagIdDisplay = ({ task }: { task: Task }) => {
    if (!task.tagId) return <Typography key={task.id + 'tagId'}>{'N/A'}</Typography>

    if (task.tagLink)
        return (
            <Typography key={task.id + 'tagId'} link href={task.tagLink} target="_blank">
                {task.tagId!}
            </Typography>
        )
    else return <Typography key={task.id + 'tagId'}>{task.tagId!}</Typography>
}

export const DescriptionDisplay = ({ task }: { task: Task }) => {
    if (!task.description) return <Typography key={task.id + 'descr'}>{'N/A'}</Typography>
    return <Typography key={task.id + 'descr'}>{task.description}</Typography>
}
