import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionDefinitionTaskTable, TaskAndData, TaskTable } from './TaskOverview/TaskTable'
import { useLanguageContext } from 'contexts/LanguageContext'
import { MissionDefinitionPlantMap, PlantMap } from './MapPosition/PointillaMapView'
import { ContentCard, StyledTableAndMap } from 'components/Styles/StyledComponents'
import { MissionDefinition } from 'models/MissionDefinition'
import { RobotWithoutTelemetry } from 'models/Robot'

const TaskAndMapSection = styled(ContentCard)`
    min-height: 60%;
    align-self: stretch;
`

interface TaskTableAndMapProps {
    tasksAndData: TaskAndData[]
    plantCode: string
    robot: RobotWithoutTelemetry
}

interface MissionDefinitionTaskTableAndMapProps {
    missionDefinition: MissionDefinition
}

export const TaskTableAndMap = ({ tasksAndData, plantCode, robot }: TaskTableAndMapProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <TaskAndMapSection>
            <Typography variant="h4">{TranslateText('Tasks')}</Typography>
            <StyledTableAndMap>
                <TaskTable tasksAndData={tasksAndData} />
                {plantCode && (
                    <PlantMap plantCode={plantCode} floorId="0" tasks={tasksAndData.map((t) => t.task)} robot={robot} />
                )}
            </StyledTableAndMap>
        </TaskAndMapSection>
    )
}

export const MissionDefinitionTaskTableAndMap = ({ missionDefinition }: MissionDefinitionTaskTableAndMapProps) => {
    const { TranslateText } = useLanguageContext()
    const plantCode = missionDefinition.inspectionArea.plantCode
    return (
        <TaskAndMapSection>
            <Typography variant="h4">{TranslateText('Tasks')}</Typography>
            <StyledTableAndMap>
                <MissionDefinitionTaskTable tasks={missionDefinition.tasks} />
                {plantCode && (
                    <MissionDefinitionPlantMap plantCode={plantCode} floorId="0" tasks={missionDefinition.tasks} />
                )}
            </StyledTableAndMap>
        </TaskAndMapSection>
    )
}
