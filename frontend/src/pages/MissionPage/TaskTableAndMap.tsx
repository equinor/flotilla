import { Typography } from '@equinor/eds-core-react'
import { ReactNode } from 'react'
import { MissionDefinitionTaskTable, TaskAndData, TaskTable } from './TaskOverview/TaskTable'
import { useLanguageContext } from 'contexts/LanguageContext'
import { MissionDefinitionPlantMap, PlantMap } from './MapPosition/PointillaMapView'
import { TableAndMapLayout } from 'components/Layouts/TableAndMapLayout'
import { MissionDefinition } from 'models/MissionDefinition'
import { RobotWithoutTelemetry } from 'models/Robot'

interface TaskTableAndMapProps {
    tasksAndData: TaskAndData[]
    plantCode: string
    robot: RobotWithoutTelemetry
    children?: ReactNode
}

interface MissionDefinitionTaskTableAndMapProps {
    missionDefinition: MissionDefinition
}

export const TaskTableAndMap = ({ tasksAndData, plantCode, robot, children }: TaskTableAndMapProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <TableAndMapLayout
            title={<Typography variant="h4">{TranslateText('Tasks')}</Typography>}
            table={<TaskTable tasksAndData={tasksAndData} />}
            map={
                plantCode && (
                    <PlantMap plantCode={plantCode} floorId="0" tasks={tasksAndData.map((t) => t.task)} robot={robot} />
                )
            }
        >
            {children}
        </TableAndMapLayout>
    )
}

export const MissionDefinitionTaskTableAndMap = ({ missionDefinition }: MissionDefinitionTaskTableAndMapProps) => {
    const { TranslateText } = useLanguageContext()
    const plantCode = missionDefinition.inspectionArea.plantCode
    return (
        <TableAndMapLayout
            title={<Typography variant="h4">{TranslateText('Tasks')}</Typography>}
            table={<MissionDefinitionTaskTable tasks={missionDefinition.tasks} />}
            map={
                plantCode && (
                    <MissionDefinitionPlantMap plantCode={plantCode} floorId="0" tasks={missionDefinition.tasks} />
                )
            }
        />
    )
}
