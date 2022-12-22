import { Table, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { IsarTask } from 'models/IsarTask'
import { EchoTag } from 'models/EchoMission'

const StyledTable = styled(Table)`
    grid-column: 1/ -1;
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
                <Typography variant="h3">Tasks</Typography>
            </Table.Caption>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>#</Table.Cell>
                    <Table.Cell>Tag-ID</Table.Cell>
                    <Table.Cell>Inspection Type</Table.Cell>
                    <Table.Cell>Status</Table.Cell>
                    <Table.Cell>Echo Link</Table.Cell>
                    <Table.Cell>Data Link</Table.Cell>
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
                <Table.Cell>{task.tagId}</Table.Cell>
                <Table.Cell>{task.steps[0].inspectionType}</Table.Cell>
                <Table.Cell>{task.taskStatus}</Table.Cell>
                <Table.Cell>{}</Table.Cell>
                <Table.Cell>{}</Table.Cell>
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
                    <Table.Cell>{task.tagId}</Table.Cell>
                    <Table.Cell>{inspection.inspectionType}</Table.Cell>
                    <Table.Cell>Planned</Table.Cell>
                    <Table.Cell>{task.url}</Table.Cell>
                    <Table.Cell>-</Table.Cell>
                </Table.Row>
            )
        })
        return inspections
    })
    return rows
}
