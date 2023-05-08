import { Table, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { TaskStatusDisplay } from './TaskStatusDisplay'
import { translateText } from 'components/Contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import { tokens } from '@equinor/eds-tokens'
import { GetColorsFromTaskStatus } from '../MarkerStyles'

const StyledTable = styled(Table)`
    grid-column: 1/ -1;
    font: equinor;
`

const Circle = styled.div`
    border-radius: 50%;
    text-align: center;
    min-width: 1.2rem;
`

interface MissionProps {
    mission?: Mission
}

export function TaskTable({ mission }: MissionProps) {
    var rows
    if (mission && mission.tasks.length > 0) rows = renderTasks(mission.tasks)

    return (
        <StyledTable>
            <Table.Caption>
                <Typography variant="h2">{translateText('Tasks')}</Typography>
            </Table.Caption>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>#</Table.Cell>
                    <Table.Cell>{translateText('Tag-ID')}</Table.Cell>
                    <Table.Cell>{translateText('Description')}</Table.Cell>
                    <Table.Cell>{translateText('Inspection Types')}</Table.Cell>
                    <Table.Cell>{translateText('Status')}</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>{rows}</Table.Body>
        </StyledTable>
    )
}

function renderTasks(tasks: Task[]) {
    var rows = tasks?.map(function (task) {
        // Workaround for current bug in echo
        var order: number = task.taskOrder < 214748364 ? task.taskOrder + 1 : 1
        const rowStyle =
            task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
                ? { background: tokens.colors.infographic.primary__mist_blue.hex }
                : {}
        const markerColors = GetColorsFromTaskStatus(task.status)
        return (
            <Table.Row key={order} style={rowStyle}>
                <Table.Cell>
                    <Circle style={{ background: markerColors.fillColor, color: markerColors.textColor }}>
                        {order}
                    </Circle>
                </Table.Cell>
                <Table.Cell> {renderTagId(task)}</Table.Cell>
                <Table.Cell> {renderDescription(task)}</Table.Cell>
                <Table.Cell> {renderInspectionTypes(task)} </Table.Cell>
                <Table.Cell>
                    <TaskStatusDisplay status={task.status} />
                </Table.Cell>
            </Table.Row>
        )
    })
    return rows
}

function renderTagId(task: Task) {
    if (!task.tagId) return <Typography>{'N/A'}</Typography>

    if (task.echoTagLink)
        return (
            <Typography link href={task.echoTagLink} target="_blank">
                {task.tagId!}
            </Typography>
        )
    else return <Typography>{task.tagId!}</Typography>
}

function renderDescription(task: Task) {
    if (!task.description) return <Typography>{'N/A'}</Typography>
    return <Typography>{task.description}</Typography>
}

function renderInspectionTypes(task: Task) {
    return task.inspections?.map(function (inspection) {
        if (inspection.inspectionUrl)
            return (
                <Typography link href={inspection.inspectionUrl}>
                    {translateText(inspection.inspectionType as string)}
                </Typography>
            )
        else return <Typography>{translateText(inspection.inspectionType as string)}</Typography>
    })
}
