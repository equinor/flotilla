import { Button, Chip, Table, Typography } from '@equinor/eds-core-react'
import { TaskStatusDisplay } from './TaskStatusDisplay'
import { TaskAnalysisDisplay } from './TaskAnalysisDisplay'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import { tokens } from '@equinor/eds-tokens'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'
import { ValidInspectionReportInspectionTypes } from 'models/Inspection'
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'
import { DescriptionDisplay, TagIdDisplay } from 'components/Displays/TaskDisplay'
import { StyledTable, StyledTableBody, StyledTableCell, StyledTableRow } from 'components/Styles/StyledComponents'
import { MissionTaskDefinition } from 'models/MissionDefinition'

interface TaskTableProps {
    tasks: Task[]
}

interface MissionDefinitionTaskTableProps {
    tasks: MissionTaskDefinition[]
}

export const TaskTable = ({ tasks }: TaskTableProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <StyledTableCell>#</StyledTableCell>
                    <StyledTableCell>{TranslateText('Tag-ID')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Description')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Inspection Types')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Status')}</StyledTableCell>
                    {tasks.some((t) => t.inspection.analysisResult) && (
                        <StyledTableCell>{TranslateText('Analysis')}</StyledTableCell>
                    )}
                </Table.Row>
            </Table.Head>
            <StyledTableBody>{tasks && <TaskTableRows tasks={tasks} />}</StyledTableBody>
        </StyledTable>
    )
}

export const MissionDefinitionTaskTable = ({ tasks }: MissionDefinitionTaskTableProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <StyledTableCell>#</StyledTableCell>
                    <StyledTableCell>{TranslateText('Tag-ID')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Description')}</StyledTableCell>
                </Table.Row>
            </Table.Head>
            <StyledTableBody>{tasks && <MissionDefinitionTaskTableRows tasks={tasks} />}</StyledTableBody>
        </StyledTable>
    )
}

const TaskTableRows = ({ tasks }: TaskTableProps) => {
    const rows = tasks.map((task, index) => {
        const order: number = index + 1
        const rowStyle =
            task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
                ? { background: tokens.colors.infographic.primary__mist_blue.hex }
                : task.inspection.analysisResult?.warning
                  ? { background: tokens.colors.interactive.danger__highlight.hex }
                  : {}
        const markerColors = getColorsFromTaskStatus(task.status)

        return (
            <StyledTableRow key={task.id} style={rowStyle}>
                <Table.Cell>
                    <Chip style={{ background: markerColors.fillColor }}>
                        <Typography variant="body_short_bold" style={{ color: markerColors.textColor }}>
                            {order}
                        </Typography>
                    </Chip>
                </Table.Cell>
                <Table.Cell>
                    <TagIdDisplay task={task} index={index} />
                </Table.Cell>
                <Table.Cell>
                    <DescriptionDisplay task={task} index={index} />
                </Table.Cell>
                <Table.Cell>
                    <InspectionTypesDisplay task={task} />
                </Table.Cell>
                <Table.Cell>
                    <TaskStatusDisplay status={task.status} errorMessage={task.errorDescription} />
                </Table.Cell>
                {tasks.some((t) => t.inspection.analysisResult) && (
                    <Table.Cell>
                        {task.inspection.analysisResult ? <TaskAnalysisDisplay task={task} /> : <></>}
                    </Table.Cell>
                )}
            </StyledTableRow>
        )
    })
    return <>{rows}</>
}

const MissionDefinitionTaskTableRows = ({ tasks }: MissionDefinitionTaskTableProps) => {
    const rows = tasks.map((task, index) => {
        const order: number = index + 1

        return (
            <StyledTableRow key={index + 'missionDefintion'}>
                <Table.Cell>
                    <Chip>
                        <Typography variant="body_short_bold">{order}</Typography>
                    </Chip>
                </Table.Cell>
                <Table.Cell>
                    <TagIdDisplay task={task} index={index} />
                </Table.Cell>
                <Table.Cell>
                    <DescriptionDisplay task={task} index={index} />
                </Table.Cell>
            </StyledTableRow>
        )
    })
    return <>{rows}</>
}

interface InspectionTypesDisplayProps {
    task: Task
}

const InspectionTypesDisplay = ({ task }: InspectionTypesDisplayProps) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionId } = useInspectionId()

    return (
        <>
            {task.inspection &&
                (ValidInspectionReportInspectionTypes.includes(task.inspection.inspectionType) &&
                task.status === TaskStatus.Successful ? (
                    <Button
                        key={task.id + task.inspection.isarInspectionId + 'insp'}
                        variant="ghost"
                        onClick={() => switchSelectedInspectionId(task.inspection.isarInspectionId)}
                        style={{ padding: 0 }}
                    >
                        <Typography variant="body_short_link">
                            {TranslateText(task.inspection.inspectionType as string)}
                        </Typography>
                    </Button>
                ) : (
                    <Typography key={task.id + task.inspection.isarInspectionId + 'insp'}>
                        {TranslateText(task.inspection.inspectionType as string)}
                    </Typography>
                ))}
        </>
    )
}
