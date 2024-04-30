import { Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { tokens } from '@equinor/eds-tokens'

const TableWithHeader = styled.div`
    display: grid;
    grid-columns: auto;
    gap: 1rem;
    margin: 2rem 0rem;
    max-width: 25rem;
`

const StyledTable = styled.div`
    display: grid;
    overflow-x: auto;
    overflow-y: hidden;
`

const StyledTableRow = styled(Table.Row)`
    &:nth-child(1),
    &:nth-child(5) {
        background-color: ${tokens.colors.interactive.danger__highlight.hex};
    }
    &:nth-child(2),
    &:nth-child(4) {
        background-color: ${tokens.colors.interactive.warning__highlight.hex};
    }
    &:nth-child(3) {
        background-color: ${tokens.colors.interactive.success__highlight.hex};
    }
`

export const PressureTable = () => {
    const { TranslateText } = useLanguageContext()
    return (
        <>
            <TableWithHeader>
                <Typography variant="h2">{TranslateText('Pressure')}</Typography>
                <Typography>{TranslateText('Recommended pressure')}</Typography>
                <StyledTable>
                    <Table>
                        <Table.Head sticky>
                            <Table.Row>
                                <Table.Cell>{TranslateText('Pressure level')}</Table.Cell>
                                <Table.Cell>{TranslateText('Pressure limit')}</Table.Cell>
                            </Table.Row>
                        </Table.Head>
                        <Table.Body>
                            <StyledTableRow>
                                <Table.Cell>{TranslateText('HighHigh')}</Table.Cell>
                                <Table.Cell>{'> 90'}</Table.Cell>
                            </StyledTableRow>
                            <StyledTableRow>
                                <Table.Cell>{TranslateText('High')}</Table.Cell>
                                <Table.Cell>{'75-90'}</Table.Cell>
                            </StyledTableRow>
                            <StyledTableRow>
                                <Table.Cell>{TranslateText('Ok')}</Table.Cell>
                                <Table.Cell>{'25-75'}</Table.Cell>
                            </StyledTableRow>
                            <StyledTableRow>
                                <Table.Cell>{TranslateText('Low')}</Table.Cell>
                                <Table.Cell>{'10-25'}</Table.Cell>
                            </StyledTableRow>
                            <StyledTableRow>
                                <Table.Cell>{TranslateText('LowLow')}</Table.Cell>
                                <Table.Cell>{'< 10'}</Table.Cell>
                            </StyledTableRow>
                        </Table.Body>
                    </Table>
                </StyledTable>
            </TableWithHeader>
        </>
    )
}
