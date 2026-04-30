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
import { StyledTable } from 'components/Styles/StyledComponents'

interface TaskTableProps {
    tasks: Task[]
    missionDefinitionPage: boolean
}

export const TaskTable = ({ tasks, missionDefinitionPage }: TaskTableProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>#</Table.Cell>
                    <Table.Cell>{TranslateText('Tag-ID')}</Table.Cell>
                    <Table.Cell>{TranslateText('Description')}</Table.Cell>
                    {!missionDefinitionPage && (
                        <>
                            <Table.Cell>{TranslateText('Inspection Types')}</Table.Cell>
                            <Table.Cell>{TranslateText('Status')}</Table.Cell>
                            {tasks.some((t) => t.inspection.analysisResult) && (
                                <Table.Cell>{TranslateText('Analysis')}</Table.Cell>
                            )}
                        </>
                    )}
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {tasks && <TaskTableRows tasks={tasks} missionDefinitionPage={missionDefinitionPage} />}
            </Table.Body>
        </StyledTable>
    )
}

const TaskTableRows = ({ tasks, missionDefinitionPage }: TaskTableProps) => {
    const rows = tasks.map((task) => {
        // Workaround for current bug in echo
        const order: number = task.taskOrder + 1
        const rowStyle =
            task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
                ? { background: tokens.colors.infographic.primary__mist_blue.hex }
                : task.inspection.analysisResult?.warning
                  ? { background: tokens.colors.interactive.danger__highlight.hex }
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
                {!missionDefinitionPage && (
                    <>
                        <Table.Cell>
                            <InspectionTypesDisplay task={task} />
                        </Table.Cell>
                        <Table.Cell>
                            <TaskStatusDisplay status={task.status} errorMessage={task.errorDescription} />
                        </Table.Cell>
                        <Table.Cell>
                            {task.inspection.analysisResult ? <TaskAnalysisDisplay task={task} /> : <></>}
                        </Table.Cell>
                    </>
                )}
            </Table.Row>
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
