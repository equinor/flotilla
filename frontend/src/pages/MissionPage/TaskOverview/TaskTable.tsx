import { Button, Chip, Icon, Table, Typography } from '@equinor/eds-core-react'
import { TaskStatusDisplay } from './TaskStatusDisplay'
import { useLanguageContext } from 'contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import { tokens } from '@equinor/eds-tokens'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'
import { AnalysisValueDisplay, DescriptionDisplay, TagIdDisplay } from 'components/Displays/TaskDisplay'
import { StyledTable, StyledTableBody, StyledTableCell, StyledTableRow } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { MissionTaskDefinition } from 'models/MissionDefinition'
import { hasInspectionAnalysis, InspectionData, TaskMeasurement } from 'models/InspectionRecord'
import { Icons } from 'utils/icons'
import { getAnalysisResultStyle } from '../AnalysisResultStyles'
import { MeasurementDisplay } from 'components/Displays/MeasurementDisplay'
import { useMissionResultSelection } from '../MissionResults/MissionResults'

const IconWithLabel = styled.div`
    display: grid;
    grid-template-columns: 1fr ${tokens.shape.icon_button.minWidth};
    align-items: center;
    text-align: right;

    > button {
        grid-column: 2;
    }
`
const RightAlignedImageButton = styled(Button)`
    margin-left: auto;
`

export interface TaskAndData {
    task: Task
    data: InspectionData | undefined
    measurements: TaskMeasurement[]
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
                    <StyledTableCell style={{ textAlign: 'right' }}>{TranslateText('Sensor Types')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Status')}</StyledTableCell>
                    <StyledTableCell style={{ textAlign: 'right' }}>{TranslateText('Analysis')}</StyledTableCell>
                </Table.Row>
            </Table.Head>
            <StyledTableBody>
                {tasksAndData &&
                    tasksAndData.map((taskAndData, index) => (
                        <TaskTableRow
                            key={taskAndData.task.id}
                            task={taskAndData.task}
                            inspectionData={taskAndData.data}
                            measurements={taskAndData.measurements}
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
    measurements,
    index,
}: {
    task: Task
    inspectionData: InspectionData | undefined
    measurements: TaskMeasurement[]
    index: number
}) => {
    const { TranslateText } = useLanguageContext()
    const { select } = useMissionResultSelection()

    const isActive = task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
    const analysisStyle =
        !isActive && hasInspectionAnalysis(inspectionData)
            ? getAnalysisResultStyle(Boolean(inspectionData?.warning))
            : undefined
    const rowStyle = {
        background: isActive ? tokens.colors.infographic.primary__mist_blue.hex : analysisStyle?.background,
    }
    const rowEdge = analysisStyle?.boxShadow ?? getAnalysisResultStyle().boxShadow
    const markerColors = getColorsFromTaskStatus(task.status)

    const mediaSAS = inspectionData?.anonymizedSAS ?? inspectionData?.visualizedSAS

    return (
        <StyledTableRow style={rowStyle}>
            <Table.Cell style={{ boxShadow: isActive ? undefined : rowEdge }}>
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
                    {inspectionData && mediaSAS && measurements.length === 0 && (
                        <RightAlignedImageButton
                            variant="ghost_icon"
                            aria-label={TranslateText('Enlarge inspection for {0}', [inspectionData.tag])}
                            onClick={() => select(task.id, 'inspection')}
                        >
                            <Icon name={Icons.Image}></Icon>
                        </RightAlignedImageButton>
                    )}
                </IconWithLabel>
            </Table.Cell>
            <Table.Cell>
                <TaskStatusDisplay status={task.status} errorMessage={task.errorDescription} />
            </Table.Cell>
            <Table.Cell>
                {measurements.length === 1 && <MeasurementDisplay recordId={measurements[0].recordId} />}
                {measurements.length > 1 && (
                    <Button variant="ghost" onClick={() => select(task.id, 'inspection')}>
                        {TranslateText('{0} measurements', [String(measurements.length)])}
                        <Icon name={Icons.RightCheveron} size={16} />
                    </Button>
                )}
                <IconWithLabel>
                    {inspectionData && inspectionData.value && (
                        <AnalysisValueDisplay
                            value={inspectionData.value}
                            unit={inspectionData.unit}
                            analysisType={inspectionData.analysisType}
                        />
                    )}
                    {inspectionData && inspectionData.visualizedSAS && (
                        <Button variant="ghost_icon" onClick={() => select(task.id, 'analysis')}>
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
