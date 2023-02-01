import { Table, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { IsarTask, IsarTaskStatus } from 'models/IsarTask'
import { EchoTag } from 'models/EchoMission'
import { TaskStatusDisplay } from './TaskStatusDisplay'

const StyledTable = styled(Table)`
    grid-column: 1/ -1;
    font: equinor;
`
interface MissionProps {
    mission?: Mission
}

export function TaskTable({ mission }: MissionProps) {
    var rows
    if (mission && mission.tasks.length > 0) rows = renderOngoingTasks(mission.tasks)
    else if (mission && mission.plannedTasks.length > 0) rows = renderUpcomingTasks(mission.plannedTasks)

    return (
        <StyledTable>
            <Table.Caption>
                <Typography variant="h2">Tasks</Typography>
            </Table.Caption>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>#</Table.Cell>
                    <Table.Cell>Tag-ID</Table.Cell>
                    <Table.Cell>Description</Table.Cell>
                    <Table.Cell>Inspection Type</Table.Cell>
                    <Table.Cell>Status</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>{rows}</Table.Body>
        </StyledTable>
    )
}

function renderOngoingTasks(tasks: IsarTask[]) {
    var indexCounter = 0
    var rows = tasks?.map(function (task) {
        indexCounter++
        return (
            <Table.Row key={indexCounter}>
                <Table.Cell>{indexCounter}</Table.Cell>
                <Table.Cell> {task.tagId}</Table.Cell>
                <Table.Cell> - </Table.Cell>
                {task.taskStatus === IsarTaskStatus.Successful && (
                    <Table.Cell>
                        <Typography link href={task.steps[0].fileLocation}>
                            {task.steps[0].inspectionType}
                        </Typography>
                    </Table.Cell>
                )}
                {task.taskStatus !== IsarTaskStatus.Successful && (
                    <Table.Cell>
                        <Typography>{task.steps[0].inspectionType}</Typography>
                    </Table.Cell>
                )}
                <Table.Cell>
                    <TaskStatusDisplay status={task.taskStatus} />
                </Table.Cell>
            </Table.Row>
        )
    })
    return rows
}

function renderUpcomingTasks(tasks: EchoTag[]) {
    var indexCounter = 0
    var rows = tasks?.map(function (task) {
        var inspections = task.inspections?.map(function (inspection) {
            indexCounter++
            return (
                <Table.Row key={indexCounter}>
                    <Table.Cell>{indexCounter}</Table.Cell>
                    <Table.Cell>
                        <Typography link href={task.url}>
                            {task.tagId}
                        </Typography>
                    </Table.Cell>
                    <Table.Cell> - </Table.Cell>
                    <Table.Cell>{inspection.inspectionType}</Table.Cell>
                    <Table.Cell>
                        <TaskStatusDisplay status={IsarTaskStatus.NotStarted} />
                    </Table.Cell>
                </Table.Row>
            )
        })
        return inspections
    })
    return rows
}
