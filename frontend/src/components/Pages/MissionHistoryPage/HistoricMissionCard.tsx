import { Button, Table, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { Mission } from 'models/Mission'
import { MissionStatusDisplay } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { TaskStatus } from 'models/Task'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'

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
                <Typography>{format(new Date(mission.endTime), 'HH:mm:ss - dd/MM/yy')}</Typography>
            ) : (
                <Typography>-</Typography>
            )}
        </>
    )
}

export const HistoricMissionCard = ({ index, mission }: IndexedMissionProps) => {
    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }

    const missionHasFailedTasks = mission.tasks.some(
        (t) => t.status !== TaskStatus.PartiallySuccessful && t.status !== TaskStatus.Successful
    )

    return (
        <Table.Row key={index}>
            <Table.Cell>
                <MissionStatusDisplay status={mission.status} />
            </Table.Cell>
            <Table.Cell>
                <Button as={Typography} variant="ghost" onClick={routeChange}>
                    {mission.name}
                </Button>
            </Table.Cell>
            <Table.Cell>
                <Typography>{mission.area?.areaName}</Typography>
            </Table.Cell>
            <Table.Cell>
                <Typography>{mission.robot.name}</Typography>
            </Table.Cell>
            <Table.Cell>
                <MissionEndTimeDisplay mission={mission} />
            </Table.Cell>
            <Table.Cell>
                <MissionRestartButton mission={mission} hasFailedTasks={missionHasFailedTasks} />
            </Table.Cell>
        </Table.Row>
    )
}
