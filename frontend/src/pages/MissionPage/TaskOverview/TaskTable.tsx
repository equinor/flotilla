import { Button, Chip, Table, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { TaskStatusDisplay } from './TaskStatusDisplay'
import { TaskAnalysisDisplay } from './TaskAnalysisDisplay'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import { tokens } from '@equinor/eds-tokens'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'
import { ValidInspectionReportInspectionTypes } from 'models/Inspection'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'

const StyledTable = styled(Table)`
    display: block;
    overflow: auto;
    max-width: calc(80vw);
`

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

const TagIdDisplay = ({ task }: { task: Task }) => {
    if (!task.tagId) return <Typography key={task.id + 'tagId'}>{'N/A'}</Typography>

    if (task.tagLink)
        return (
            <Typography key={task.id + 'tagId'} link href={task.tagLink} target="_blank">
                {task.tagId!}
            </Typography>
        )
    else return <Typography key={task.id + 'tagId'}>{task.tagId!}</Typography>
}

const DescriptionDisplay = ({ task }: { task: Task }) => {
    if (!task.description) return <Typography key={task.id + 'descr'}>{'N/A'}</Typography>
    return <Typography key={task.id + 'descr'}>{task.description}</Typography>
}

interface InspectionTypesDisplayProps {
    task: Task
}

const InspectionTypesDisplay = ({ task }: InspectionTypesDisplayProps) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()

    return (
        <>
            {task.inspection &&
                (ValidInspectionReportInspectionTypes.includes(task.inspection.inspectionType) &&
                task.status === TaskStatus.Successful ? (
                    <Button
                        key={task.id + task.inspection.id + 'insp'}
                        variant="ghost"
                        onClick={() => switchSelectedInspectionTask(task)}
                        style={{ padding: 0 }}
                    >
                        <Typography variant="body_short_link">
                            {TranslateText(task.inspection.inspectionType as string)}
                        </Typography>
                    </Button>
                ) : (
                    <Typography key={task.id + task.inspection.id + 'insp'}>
                        {TranslateText(task.inspection.inspectionType as string)}
                    </Typography>
                ))}
        </>
    )
}
