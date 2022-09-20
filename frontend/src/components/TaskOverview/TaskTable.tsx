import { Table, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { useAssetContext } from 'components/Contexts/AssetContext'

const StyledTable = styled(Table)`
    grid-column: 1/ -1;
`
interface TaskProps {
    mission?: Mission
}

export function TaskTable({ mission }: TaskProps) {
    const { asset, switchAsset } = useAssetContext()
    console.log(mission)
    var indexCounter = 0;
    var rows = mission?.tasks.map(function (task) {
        indexCounter++
        return (
            <Table.Row>
                <Table.Cell>{indexCounter}</Table.Cell>
                <Table.Cell>{asset}</Table.Cell>
                <Table.Cell></Table.Cell>
                <Table.Cell></Table.Cell>
                <Table.Cell></Table.Cell>
            </Table.Row>
        )
    })
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
                    <Table.Cell>Data Link</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {rows}
            </Table.Body>
        </StyledTable>
    )
}
