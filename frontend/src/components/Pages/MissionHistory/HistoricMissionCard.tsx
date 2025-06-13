import { Table, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { Mission } from 'models/Mission'
import {
    MissionStatusDisplay,
    MissionStatusDisplayShort,
} from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { TaskStatus } from 'models/Task'
import { useNavigate } from 'react-router-dom'
import { formatDateTime } from 'utils/StringFormatting'

enum InspectionTableColumns {
    StatusShort = 'StatusShort',
    Status = 'Status',
    Name = 'Name',
    Area = 'Area',
    Robot = 'Robot',
    CompletionTime = 'CompletionTime',
    Rerun = 'RerunMission',
}

interface IndexedMissionProps {
    index: number
    mission: Mission
}

interface MissionProps {
    mission: Mission
}

const MissionEndTimeDisplay = ({ mission }: MissionProps) => {
    return (
        <>
            {mission.endTime ? (
                <Typography>{formatDateTime(mission.endTime, 'HH:mm:ss - dd/MM/yy')}</Typography>
            ) : (
                <Typography>-</Typography>
            )}
        </>
    )
}

export const HistoricMissionCard = ({ index, mission }: IndexedMissionProps) => {
    const navigate = useNavigate()

    const missionHasFailedTasks = mission.tasks.some(
        (t) => t.status !== TaskStatus.PartiallySuccessful && t.status !== TaskStatus.Successful
    )

    return (
        <Table.Row key={index}>
            <Table.Cell id={InspectionTableColumns.StatusShort}>
                <MissionStatusDisplayShort status={mission.status} />
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Status}>
                <MissionStatusDisplay status={mission.status} />
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Name}>
                <Typography link onClick={() => navigate(`${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`)}>
                    {mission.name}
                </Typography>
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Robot}>
                <Typography>{mission.robot.name}</Typography>
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.CompletionTime}>
                <MissionEndTimeDisplay mission={mission} />
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Rerun}>
                <MissionRestartButton mission={mission} hasFailedTasks={missionHasFailedTasks} smallButton={true} />
            </Table.Cell>
        </Table.Row>
    )
}
