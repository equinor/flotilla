import { Button, Chip, Icon, Table, Typography } from '@equinor/eds-core-react'
import { TaskStatusDisplay } from './TaskStatusDisplay'
import { useLanguageContext } from 'contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import { tokens } from '@equinor/eds-tokens'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'
import { AnalysisValueDisplay, DescriptionDisplay, TagIdDisplay } from 'components/Displays/TaskDisplay'
import { StyledTable, StyledTableBody, StyledTableCell, StyledTableRow } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { MissionTaskDefinition } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import { Icons } from 'utils/icons'

const IconWithLabel = styled.div`
    display: flex;
    align-items: center;
`

export interface TaskAndData {
    task: Task
    data: InspectionData | undefined
}

interface TaskTableProps {
    tasksAndData: TaskAndData[]
}

interface MissionDefinitionTaskTableProps {
    tasks: MissionTaskDefinition[]
}

export const TaskTable = ({ tasksAndData }: TaskTableProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <StyledTableCell>#</StyledTableCell>
                    <StyledTableCell>{TranslateText('Tag-ID')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Description')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Sensor Types')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Status')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Analysis')}</StyledTableCell>
                </Table.Row>
            </Table.Head>
            <StyledTableBody>
                {tasksAndData &&
                    tasksAndData.map((taskAndData, index) => (
                        <TaskTableRow
                            key={taskAndData.task.id}
                            task={taskAndData.task}
                            inspectionData={taskAndData.data}
                            index={index}
                        />
                    ))}
            </StyledTableBody>
        </StyledTable>
    )
}

const TaskTableRow = ({
    task,
    inspectionData,
    index,
}: {
    task: Task
    inspectionData: InspectionData | undefined
    index: number
}) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionId, switchSelectedAnalysisId } = useInspectionId()

    const rowStyle =
        task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
            ? { background: tokens.colors.infographic.primary__mist_blue.hex }
            : inspectionData?.warning
              ? { background: tokens.colors.interactive.danger__highlight.hex }
              : {}
    const markerColors = getColorsFromTaskStatus(task.status)

    return (
        <StyledTableRow style={rowStyle}>
            <Table.Cell>
                <Chip style={{ background: markerColors.fillColor }}>
                    <Typography variant="body_short_bold" style={{ color: markerColors.textColor }}>
                        {index + 1}
                    </Typography>
                </Chip>
            </Table.Cell>
            <Table.Cell>
                <TagIdDisplay tagId={task.tagId} index={index} />
            </Table.Cell>
            <Table.Cell>
                <DescriptionDisplay description={task.description} index={index} />
            </Table.Cell>
            <Table.Cell>
                <IconWithLabel>
                    <Typography>{TranslateText(task.sensorType as string)}</Typography>
                    {inspectionData && inspectionData.anonymizedSAS && (
                        <Button variant="ghost_icon" onClick={() => switchSelectedInspectionId(task.id)}>
                            <Icon name={Icons.Image}></Icon>
                        </Button>
                    )}
                </IconWithLabel>
            </Table.Cell>
            <Table.Cell>
                <TaskStatusDisplay status={task.status} errorMessage={task.errorDescription} />
            </Table.Cell>
            <Table.Cell>
                <IconWithLabel>
                    {inspectionData && inspectionData.value && (
                        <AnalysisValueDisplay
                            value={inspectionData.value}
                            unit={inspectionData.unit}
                            analysisType={inspectionData.analysisType}
                        />
                    )}
                    {inspectionData && inspectionData.visualizedSAS && (
                        <Button variant="ghost_icon" onClick={() => switchSelectedAnalysisId(task.id)}>
                            <Icon name={Icons.Image}></Icon>
                        </Button>
                    )}
                </IconWithLabel>
            </Table.Cell>
        </StyledTableRow>
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
                    <TagIdDisplay tagId={task.tagId} index={index} />
                </Table.Cell>
                <Table.Cell>
                    <DescriptionDisplay description={task.description} index={index} />
                </Table.Cell>
            </StyledTableRow>
        )
    })
    return <>{rows}</>
}
