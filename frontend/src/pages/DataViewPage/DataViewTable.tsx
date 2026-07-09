import { Chip, Table, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StyledTable, StyledTableCell } from 'components/Styles/StyledComponents'
import { DescriptionDisplay, TagIdDisplay } from 'components/Displays/TaskDisplay'
import { formatDateTime } from 'utils/StringFormatting'
import { InspectionData } from 'models/InspectionRecord'

const unitDisplaySymbols: Record<string, string> = {
    celsius: '°C',
    percentage: '%',
}

const formatUnit = (unit?: string): string => {
    if (!unit) return ''
    const normalizedUnit = unit
        .toLowerCase()
        .replace(/\s*\[.*\]\s*/, '')
        .trim()
    return unitDisplaySymbols[normalizedUnit] ?? unit
}

export const DataViewTable = ({
    inspectionData,
    selectedInspectionId,
    onSelectInspection,
}: {
    inspectionData: InspectionData[]
    selectedInspectionId: string | undefined
    onSelectInspection: (inspectionId: string | undefined) => void
}) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <StyledTableCell>#</StyledTableCell>
                    <StyledTableCell>{TranslateText('Tag-ID')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Description')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Latest Value')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Timestamp')}</StyledTableCell>
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {inspectionData &&
                    inspectionData.map((inspection, index) => {
                        const isSelected = inspection.inspectionId === selectedInspectionId
                        const taskHasWarning = inspection.warning
                        const backgroundColor = isSelected
                            ? tokens.colors.interactive.primary__selected_highlight.hex
                            : taskHasWarning
                              ? tokens.colors.interactive.danger__highlight.hex
                              : undefined
                        return (
                            <Table.Row
                                key={inspection.inspectionId + 'data view table row'}
                                onClick={() => onSelectInspection(isSelected ? undefined : inspection.inspectionId)}
                                style={{
                                    cursor: inspection.tag ? 'pointer' : 'default',
                                    backgroundColor,
                                }}
                            >
                                <Table.Cell>
                                    <Chip>
                                        <Typography variant="body_short_bold">{index + 1}</Typography>
                                    </Chip>
                                </Table.Cell>
                                <Table.Cell>
                                    <TagIdDisplay tagId={inspection.tag} index={index} />
                                </Table.Cell>
                                <Table.Cell>
                                    <DescriptionDisplay description={inspection.inspectionDescription} index={index} />
                                </Table.Cell>
                                {inspection.value ? (
                                    <Table.Cell>
                                        <Typography>
                                            {Math.round(parseFloat(inspection.value)) + formatUnit(inspection.unit)}
                                        </Typography>
                                    </Table.Cell>
                                ) : (
                                    <Table.Cell>
                                        <Typography>{TranslateText('Not available')}</Typography>
                                    </Table.Cell>
                                )}
                                {inspection.createdAt && (
                                    <Table.Cell>
                                        <Typography>{formatDateTime(inspection.createdAt)}</Typography>
                                    </Table.Cell>
                                )}
                            </Table.Row>
                        )
                    })}
            </Table.Body>
        </StyledTable>
    )
}
