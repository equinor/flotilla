import { Table, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Mission } from 'models/Mission'
import {
    MissionStatusDisplay,
    MissionStatusDisplayShort,
} from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { TaskStatus } from 'models/Task'
import { useNavigate } from 'react-router-dom'
import { formatDateTime } from 'utils/StringFormatting'
import { useContext } from 'react'
import { InstallationContext } from 'components/Contexts/InstallationContext'

enum InspectionTableColumns {
    StatusShort = 'StatusShort',
    Status = 'Status',
    Name = 'Name',
    Area = 'Area',
    Robot = 'Robot',
    CompletionTime = 'CompletionTime',
    Rerun = 'RerunMission',
}

const StyledRow = styled(Table.Row)`
    transition: background-color 0.12s ease;
    &&:hover {
        background-color: ${tokens.colors.ui.background__light.hex};
    }
`

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

export const HistoricMissionCard = ({ mission }: MissionProps) => {
    const navigate = useNavigate()
    const { installation } = useContext(InstallationContext)

    const missionHasFailedTasks = mission.tasks.some(
        (t) => t.status !== TaskStatus.PartiallySuccessful && t.status !== TaskStatus.Successful
    )

    const missionHasAnalysisResultWithWarning = mission.tasks.some((t) => !!t.inspection.analysisResult?.warning)

    return (
        <StyledRow
            key={mission.id}
            style={{
                backgroundColor: missionHasAnalysisResultWithWarning
                    ? tokens.colors.interactive.warning__highlight.hex
                    : undefined,
            }}
        >
            <Table.Cell id={InspectionTableColumns.StatusShort}>
                <MissionStatusDisplayShort status={mission.status} />
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Status}>
                <MissionStatusDisplay status={mission.status} />
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Name}>
                <Typography link onClick={() => navigate(`/${installation.installationCode}/mission/${mission.id}`)}>
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
        </StyledRow>
    )
}

export const SimpleHistoricMissionCard = ({ mission }: MissionProps) => {
    const navigate = useNavigate()
    const { installation } = useContext(InstallationContext)

    return (
        <StyledRow key={mission.id}>
            <Table.Cell id={InspectionTableColumns.Status}>
                <MissionStatusDisplay status={mission.status} />
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Name}>
                <Typography
                    link
                    onClick={() => navigate(`/${installation.installationCode}/mission-simple?id=${mission.id}`)}
                >
                    {mission.name}
                </Typography>
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.CompletionTime}>
                <MissionEndTimeDisplay mission={mission} />
            </Table.Cell>
        </StyledRow>
    )
}
