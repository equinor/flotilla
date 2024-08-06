import { Chip, Table, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { TaskStatusDisplay } from './TaskStatusDisplay'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import { tokens } from '@equinor/eds-tokens'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'

const StyledTable = styled(Table)`
    display: block;
    overflow: auto;
    max-width: calc(80vw);
`

const StyledTypography = styled(Typography)`
    font-family: Equinor;
    font-size: 28px;
    font-style: normal;
    line-height: 35px;

    @media (max-width: 500px) {
        font-family: Equinor;
        font-size: 24px;
        font-style: normal;
        line-height: 30px;
    }

    padding-bottom: 10px;
`

export const TaskTable = ({ tasks }: { tasks: Task[] | undefined }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledTable>
            <Table.Caption>
                <StyledTypography variant="h2">{TranslateText('Tasks')}</StyledTypography>
            </Table.Caption>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>#</Table.Cell>
                    <Table.Cell>{TranslateText('Tag-ID')}</Table.Cell>
                    <Table.Cell>{TranslateText('Description')}</Table.Cell>
                    <Table.Cell>{TranslateText('Inspection Types')}</Table.Cell>
                    <Table.Cell>{TranslateText('Status')}</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>{tasks && <TaskTableRows tasks={tasks} />}</Table.Body>
        </StyledTable>
    )
}

const TaskTableRows = ({ tasks }: { tasks: Task[] }) => {
    const rows = tasks.map((task) => {
        // Workaround for current bug in echo
        const order: number = task.taskOrder + 1
        const rowStyle =
            task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
                ? { background: tokens.colors.infographic.primary__mist_blue.hex }
                : {}
        const markerColors = getColorsFromTaskStatus(task.status)
        return (
            <Table.Row key={task.id} style={rowStyle}>
                <Table.Cell>
                    <Chip style={{ background: markerColors.fillColor }}>
                        <Typography variant="body_short_bold" style={{ color: markerColors.textColor }}>
                            {order}
                        </Typography>
                    </Chip>
                </Table.Cell>
                <Table.Cell>
                    <TagIdDisplay task={task} />
                </Table.Cell>
                <Table.Cell>
                    <DescriptionDisplay task={task} />
                </Table.Cell>
                <Table.Cell>
                    <InspectionTypesDisplay task={task} />
                </Table.Cell>
                <Table.Cell>
                    <TaskStatusDisplay status={task.status} />
                </Table.Cell>
            </Table.Row>
        )
    })
    return <>{rows}</>
}

const TagIdDisplay = ({ task }: { task: Task }) => {
    if (!task.tagId) return <Typography key={task.id + 'tagId'}>{'N/A'}</Typography>

    if (task.tagLink)
        return (
            <Typography key={task.id + 'tagId'} link href={task.tagLink} target="_blank">
                {task.tagId!}
            </Typography>
        )
    else return <Typography key={task.id + 'tagId'}>{task.tagId!}</Typography>
}

const DescriptionDisplay = ({ task }: { task: Task }) => {
    if (!task.description) return <Typography key={task.id + 'descr'}>{'N/A'}</Typography>
    return <Typography key={task.id + 'descr'}>{task.description}</Typography>
}

const InspectionTypesDisplay = ({ task }: { task: Task }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <>
            {task.inspections?.map((inspection) => {
                if (inspection.inspectionUrl)
                    return (
                        <Typography key={task.id + inspection.id + 'insp'} link href={inspection.inspectionUrl}>
                            {TranslateText(inspection.inspectionType as string)}
                        </Typography>
                    )
                else
                    return (
                        <Typography key={task.id + inspection.id + 'insp'}>
                            {TranslateText(inspection.inspectionType as string)}
                        </Typography>
                    )
            })}
        </>
    )
}
