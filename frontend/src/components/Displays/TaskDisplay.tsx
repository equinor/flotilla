import { Typography } from '@equinor/eds-core-react'
import { MissionTaskDefinition } from 'models/MissionDefinition'
import { Task } from 'models/Task'

export const TagIdDisplay = ({ task, index }: { task: Task | MissionTaskDefinition; index: number }) => {
    if (!task.tagId) return <Typography key={index + 'tagId'}>{'N/A'}</Typography>
    else return <Typography key={index + 'tagId'}>{task.tagId!}</Typography>
}

export const DescriptionDisplay = ({ task, index }: { task: Task | MissionTaskDefinition; index: number }) => {
    if (!task.description) return <Typography key={index + 'descr'}>{'N/A'}</Typography>
    return <Typography key={index + 'descr'}>{task.description}</Typography>
}
