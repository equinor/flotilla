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
import { StyledTable, StyledTableBody } from 'components/Styles/StyledComponents'
import styled from 'styled-components'

const TaskHeaderCell = styled(Table.Cell)`
    && {
        font-family: Equinor, sans-serif;
        font-size: 0.65rem;
        font-weight: 600;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: ${tokens.colors.text.static_icons__default.hex};
        border-bottom: 2px solid ${tokens.colors.ui.background__medium.hex};
    }
`

interface TaskTableProps {
    tasks: Task[]
    missionDefinitionPage: boolean
}

const StyledRow = styled(Table.Row)`
    transition: background-color 0.12s ease;
    &&:hover {
        background-color: ${tokens.colors.ui.background__light.hex};
    }
`

export const TaskTable = ({ tasks, missionDefinitionPage }: TaskTableProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <TaskHeaderCell>#</TaskHeaderCell>
                    <TaskHeaderCell>{TranslateText('Tag-ID')}</TaskHeaderCell>
                    <TaskHeaderCell>{TranslateText('Description')}</TaskHeaderCell>
                    {!missionDefinitionPage && (
                        <>
                            <TaskHeaderCell>{TranslateText('Inspection Types')}</TaskHeaderCell>
                            <TaskHeaderCell>{TranslateText('Status')}</TaskHeaderCell>
                            {tasks.some((t) => t.inspection.analysisResult) && (
                                <TaskHeaderCell>{TranslateText('Analysis')}</TaskHeaderCell>
                            )}
                        </>
                    )}
                </Table.Row>
            </Table.Head>
            <StyledTableBody>
                {tasks && <TaskTableRows tasks={tasks} missionDefinitionPage={missionDefinitionPage} />}
            </StyledTableBody>
        </StyledTable>
    )
}

const TaskTableRows = ({ tasks, missionDefinitionPage }: TaskTableProps) => {
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
            <StyledRow key={task.id} style={rowStyle}>
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
                        {tasks.some((t) => t.inspection.analysisResult) && (
                            <Table.Cell>
                                {task.inspection.analysisResult ? <TaskAnalysisDisplay task={task} /> : <></>}
                            </Table.Cell>
                        )}
                    </>
                )}
            </StyledRow>
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
