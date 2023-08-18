import { Table, Card, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Area } from 'models/Area'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { RefreshProps } from './InspectionPage'
import { tokens } from '@equinor/eds-tokens'
import { MissionDefinition } from 'models/MissionDefinition'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { Icons } from 'utils/icons'

const StyledCard = styled(Card)`
    width: 200px;
    padding: 8px;
    :hover {
        background-color: #deedee;
    }
`

const StyledAreaCards = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
`

const TableWithHeader = styled.div`
    gap: 2rem;
`

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 4rem;
`

const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    height: 100%;
    width: 100%;
    scale: 50%;
`

const Circle = (fill: string) => (
    <svg xmlns="http://www.w3.org/2000/svg" width="13" height="14" viewBox="0 0 13 14" fill="none">
        <circle cx="6.5" cy="7" r="6.5" fill={fill} />
    </svg>
)
const RedCircle = Circle('#EB0000')
const YellowCircle = Circle('#FF9200')
const GreenCircle = Circle('#4BB748')

interface AreaMissionType {
    [areaId: string]: { missionDefinitions: MissionDefinition[]; area: Area }
}

const formatBackendDateTimeToDate = (date: Date) => {
    return new Date(date.toString())
}

const getInspectionDeadline = (inspectionFrequency: string, lastRunTime: Date): Date => {
    const dayHourSecondsArray = inspectionFrequency.split(':')
    const days: number = +dayHourSecondsArray[0]
    const hours: number = +dayHourSecondsArray[1]
    const minutes: number = +dayHourSecondsArray[2]

    lastRunTime = formatBackendDateTimeToDate(lastRunTime)

    let deadline = lastRunTime
    deadline.setDate(deadline.getDate() + days)
    deadline.setHours(deadline.getHours() + hours)
    deadline.setMinutes(deadline.getMinutes() + minutes)
    return deadline
    // More flexibly we can also define the deadline in terms of milliseconds:
    // new Date(lastRunTime.getTime() + (1000 * 60 * days) + (1000 * 60 * 60 * hours) + (1000 * 60 * 60 * 24 * days))
}

export function AreasDialog({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [areaMissions, setAreaMissions] = useState<AreaMissionType>({})
    const [selectedArea, setSelectedArea] = useState<Area>()
    const [selectedMission, setSelectedMission] = useState<MissionDefinition>()
    const [isDialogOpen, setisDialogOpen] = useState<boolean>(false)
    let navigate = useNavigate()

    useEffect(() => {
        BackendAPICaller.getAreas().then(async (areas: Area[]) => {
            let newAreaMissions: AreaMissionType = {}
            const filteredAreas = areas.filter(
                (area) => area.installationCode.toLowerCase() === installationCode.toLowerCase()
            )
            for (const area of filteredAreas) {
                // These calls need to be made sequentially to update areaMissions safely
                let missionDefinitions = await BackendAPICaller.getMissionDefinitionsInArea(area)
                if (!missionDefinitions) missionDefinitions = []
                newAreaMissions[area.id] = { missionDefinitions: missionDefinitions, area: area }
            }
            setAreaMissions(newAreaMissions)
        })
    }, [installationCode])

    const getInspectionStatus = (inspectionFrequency: string, lastRunTime: Date) => {
        const deadlineDate = getInspectionDeadline(inspectionFrequency, lastRunTime)
        // The magical number on the right is the number of milliseconds in a day
        const deadline = new Date(deadlineDate.getTime() - new Date().getTime()).getTime() / 8.64e7

        if (deadline <= 0) {
            return (
                <>
                    {RedCircle} {TranslateText('Past deadline')}
                </>
            )
        } else if (deadline > 0 && deadline <= 1) {
            return (
                <>
                    {RedCircle} {TranslateText('Due today')}
                </>
            )
        } else if (deadline > 1 && deadline <= 7) {
            return (
                <>
                    {YellowCircle} {TranslateText('Due this week')}
                </>
            )
        } else if (deadline > 7 && deadline <= 14) {
            return (
                <>
                    {YellowCircle} {TranslateText('Due within two weeks')}
                </>
            )
        } else if (deadline > 7 && deadline <= 30) {
            return (
                <>
                    {GreenCircle} {TranslateText('Due within a month')}
                </>
            )
        }
        return (
            <>
                {GreenCircle} {TranslateText('Up to date')}
            </>
        )
    }

    const formatDateString = (dateStr: Date) => {
        let newStr = dateStr.toString()
        newStr = newStr.slice(0, newStr.length - 8)
        newStr = newStr.replaceAll('T', ' ')
        return newStr
    }

    const getInspectionRow = (mission: MissionDefinition) => {
        let status
        let lastCompleted: string = ''
        let deadline: string = ''
        if (!mission.lastRun || !mission.lastRun.endTime) {
            status = (
                <>
                    {RedCircle} {TranslateText('Not yet performed')}
                </>
            )
            lastCompleted = TranslateText('Never')
        } else if (mission.inspectionFrequency) {
            status = getInspectionStatus(mission.inspectionFrequency, mission.lastRun.endTime!)
            lastCompleted = formatDateString(mission.lastRun.endTime!)
            deadline = getInspectionDeadline(mission.inspectionFrequency, mission.lastRun.endTime!).toDateString()
        } else {
            status = TranslateText('No planned inspection')
            lastCompleted = formatDateString(mission.lastRun.endTime!)
        }
        return (
            <Table.Row key={mission.id}>
                <Table.Cell>{status}</Table.Cell>
                <Table.Cell>
                    <Typography
                        link
                        onClick={() => navigate(`${config.FRONTEND_BASE_ROUTE}/mission-definition/${mission.id}`)}
                    >
                        {mission.name}
                    </Typography>
                </Table.Cell>
                <Table.Cell>{mission.comment}</Table.Cell>
                <Table.Cell>{lastCompleted}</Table.Cell>
                <Table.Cell>{deadline}</Table.Cell>
                <Table.Cell>
                    <StyledIcon
                        color={`${tokens.colors.interactive.focus.hex}`}
                        name={Icons.AddOutlined}
                        size={16}
                        title={TranslateText('Add to queue')}
                        onClick={() => setisDialogOpen(true)}
                    />
                </Table.Cell>
            </Table.Row>
        )
    }

    const getInspectionsTable = (area: Area) => (
        <TableWithHeader>
            <Typography variant="h1">{TranslateText('Area Inspections')}</Typography>
            <Table>
                <Table.Head sticky>
                    <Table.Row>
                        <Table.Cell>{TranslateText('Status')}</Table.Cell>
                        <Table.Cell>{TranslateText('Name')}</Table.Cell>
                        <Table.Cell>{TranslateText('Description')}</Table.Cell>
                        <Table.Cell>{TranslateText('Last completed')}</Table.Cell>
                        <Table.Cell>{TranslateText('Deadline')}</Table.Cell>
                        <Table.Cell>{TranslateText('Add to queue')}</Table.Cell>
                    </Table.Row>
                </Table.Head>
                <Table.Body>
                    {areaMissions[area.id].missionDefinitions.map((mission) => getInspectionRow(mission))}
                </Table.Body>
            </Table>
        </TableWithHeader>
    )

    return (
        <>
            <StyledContent>
                <StyledAreaCards>
                    {Object.keys(areaMissions).map((areaId) => (
                        <StyledCard
                            variant="default"
                            key={areaId}
                            style={{ boxShadow: tokens.elevation.raised }}
                            onClick={() => setSelectedArea(areaMissions[areaId].area)}
                        >
                            <Typography>{areaMissions[areaId].area.areaName.toLocaleUpperCase()}</Typography>
                            <Typography>
                                {areaMissions[areaId] &&
                                    areaMissions[areaId].missionDefinitions.length > 0 &&
                                    areaMissions[areaId].missionDefinitions[0].name}
                            </Typography>
                        </StyledCard>
                    ))}
                </StyledAreaCards>
                {selectedArea && getInspectionsTable(selectedArea)}
            </StyledContent>
            {isDialogOpen && (
                    <ScheduleMissionDialog
                        mission={selectedMission!}
                        refreshInterval={refreshInterval}
                        closeDialog={() => setisDialogOpen(false)}
                    />
                )}
        </>
    )
}
