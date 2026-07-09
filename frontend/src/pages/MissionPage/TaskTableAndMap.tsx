import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { MissionDefinitionTaskTable, TaskAndData, TaskTable } from './TaskOverview/TaskTable'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { phone_width } from 'utils/constants'
import { MissionDefinitionPlantMap, PlantMap } from './MapPosition/PointillaMapView'
import { StyledTableAndMap } from 'components/Styles/StyledComponents'
import { MissionDefinition } from 'models/MissionDefinition'
import { RobotWithoutTelemetry } from 'models/Robot'

const TaskAndMapSection = styled.div`
    display: flex;
    min-height: 60%;
    padding: 24px 24px 24px 0;
    @media (max-width: ${phone_width}) {
        padding: 6px 8px 8px 0;
    }
    @media (min-width: ${phone_width}) {
        min-width: 930px;
    }
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
    gap: 8px;
    align-self: stretch;
    border-radius: 2px;
    background: ${tokens.colors.ui.background__default.rgba};
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
