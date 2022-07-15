import { Table, Typography } from "@equinor/eds-core-react";
import { Mission } from "models/mission";
import styled from "styled-components";
import { useContext } from "react";
import { useAssetContext } from "components/Contexts/AssetContext";

const StyledTable = styled(Table)`
    grid-column: 1/ -1;
`
interface TaskProps {
    tasks?: Mission[]
}

export function TaskTable({ tasks }: TaskProps) {

    const { asset, switchAsset } = useAssetContext();

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
                </Table.Row>
            </Table.Head>
            <Table.Row>
                <Table.Cell>Hello</Table.Cell>
                <Table.Cell>{asset}</Table.Cell>
            </Table.Row>
        </StyledTable>
    )
}
