import { Chip, Table, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StyledTable, StyledTableCell } from 'components/Styles/StyledComponents'
import { DescriptionDisplay, TagIdDisplay } from 'components/Displays/TaskDisplay'
import { Task } from 'models/Task'
import { formatDateTime } from 'utils/StringFormatting'

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
    tasks,
    selectedTagId,
    onSelectTag,
}: {
    tasks: Task[]
    selectedTagId: string | null
    onSelectTag: (tagId: string | null) => void
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
                {tasks &&
                    tasks.map((task, index) => {
                        const isSelected = !!task.tagId && task.tagId === selectedTagId
                        const taskHasWarning = !!task.inspection.analysisResult?.warning
                        const backgroundColor = isSelected
                            ? tokens.colors.interactive.primary__selected_highlight.hex
                            : taskHasWarning
                              ? tokens.colors.interactive.danger__highlight.hex
                              : undefined

                        return (
                            <Table.Row
                                key={task.id}
                                onClick={() => {
                                    if (!task.tagId) return
                                    onSelectTag(isSelected ? null : task.tagId)
                                }}
                                style={{
                                    cursor: task.tagId ? 'pointer' : 'default',
                                    backgroundColor,
                                }}
                            >
                                <Table.Cell>
                                    <Chip>
                                        <Typography variant="body_short_bold">{index + 1}</Typography>
                                    </Chip>
                                </Table.Cell>
                                <Table.Cell>
                                    <TagIdDisplay task={task} index={index} />
                                </Table.Cell>
                                <Table.Cell>
                                    <DescriptionDisplay task={task} index={index} />
                                </Table.Cell>
                                {task.inspection.analysisResult?.value ? (
                                    <Table.Cell>
                                        <Typography>
                                            {Math.round(parseFloat(task.inspection.analysisResult?.value)) +
                                                formatUnit(task.inspection.analysisResult?.unit)}
                                        </Typography>
                                    </Table.Cell>
                                ) : (
                                    <Table.Cell>
                                        <Typography>{TranslateText('Not available')}</Typography>
                                    </Table.Cell>
                                )}
                                {task.endTime && (
                                    <Table.Cell>
                                        <Typography>{formatDateTime(task.endTime)}</Typography>
                                    </Table.Cell>
                                )}
                            </Table.Row>
                        )
                    })}
            </Table.Body>
        </StyledTable>
    )
}
