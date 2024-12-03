import { Button, Chip, Table, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { TaskStatusDisplay } from './TaskStatusDisplay'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import { tokens } from '@equinor/eds-tokens'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'
import { StyledTableBody, StyledTableCaptionGray, StyledTableCell } from 'components/Styles/StyledComponents'
import { InspectionType } from 'models/Inspection'

const StyledTable = styled(Table)`
    display: block;
    overflow: auto;
    max-width: calc(80vw);
`
const StyledTypography = styled(Typography)`
    font-family: Equinor;
    font-size: 28px;
    font-style: normal;
    line-height: 35px;

    @media (max-width: 500px) {
        font-size: 24px;
        line-height: 30px;
    }

    padding-bottom: 10px;
`
interface TaskTableProps {
    tasks: Task[] | undefined
    setInspectionTask: (inspectionTask: Task | undefined) => void
}

export const TaskTable = ({ tasks, setInspectionTask }: TaskTableProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <StyledTable>
                <StyledTableCaptionGray>
                    <StyledTypography variant="h2">{TranslateText('Tasks')}</StyledTypography>
                </StyledTableCaptionGray>
                <Table.Head>
                    <Table.Row>
                        <StyledTableCell>#</StyledTableCell>
                        <StyledTableCell>{TranslateText('Tag-ID')}</StyledTableCell>
                        <StyledTableCell>{TranslateText('Description')}</StyledTableCell>
                        <StyledTableCell>{TranslateText('Inspection Types')}</StyledTableCell>
                        <StyledTableCell>{TranslateText('Status')}</StyledTableCell>
                    </Table.Row>
                </Table.Head>
                <StyledTableBody>
                    {tasks && <TaskTableRows tasks={tasks} setInspectionTask={setInspectionTask} />}
                </StyledTableBody>
            </StyledTable>
        </>
    )
}

interface TaskTableRowsProps {
    tasks: Task[]
    setInspectionTask: (inspectionTask: Task | undefined) => void
}

const TaskTableRows = ({ tasks, setInspectionTask }: TaskTableRowsProps) => {
    const rows = tasks.map((task) => {
        // Workaround for current bug in echo
        const order: number = task.taskOrder + 1
        const rowStyle =
            task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
                ? { background: tokens.colors.infographic.primary__mist_blue.hex }
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
                <Table.Cell>
                    <InspectionTypesDisplay task={task} setInspectionTask={setInspectionTask} />
                </Table.Cell>
                <Table.Cell>
                    <TaskStatusDisplay status={task.status} />
                </Table.Cell>
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
    setInspectionTask: (inspectionTask: Task | undefined) => void
}

const InspectionTypesDisplay = ({ task, setInspectionTask }: InspectionTypesDisplayProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            {task.inspection &&
                (task.inspection.inspectionType === InspectionType.Image ? (
                    task.status === TaskStatus.Successful ? (
                        <Button
                            key={task.id + task.inspection.id + 'insp'}
                            variant="ghost"
                            onClick={() => setInspectionTask(task)}
                        >
                            <Typography variant="body_short_link">
                                {TranslateText(task.inspection.inspectionType as string)}
                            </Typography>
                        </Button>
                    ) : (
                        <Button key={task.id + task.inspection.id + 'insp'} variant="ghost">
                            <Typography variant="body_short">
                                {TranslateText(task.inspection.inspectionType as string)}
                            </Typography>
                        </Button>
                    )
                ) : (
                    <Typography key={task.id + task.inspection.id + 'insp'}>
                        {TranslateText(task.inspection.inspectionType as string)}
                    </Typography>
                ))}
        </>
    )
}
