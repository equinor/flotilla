import { Button, ButtonGroup, Chip, Table, Typography } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useContext, useEffect, useState } from 'react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StyledCardsWidth, StyledPage, StyledTable, StyledTableAndMap } from 'components/Styles/StyledComponents'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { PlantMap } from './MissionPage/MapPosition/PointillaMapView'
import { Task } from 'models/Task'
import { formatDateTime } from 'utils/StringFormatting'
import { TimeseriesLinePlot, TimeseriesLinePlotData } from 'components/Displays/TimeseriesLinePlot'
import { DescriptionDisplay, TagIdDisplay } from 'components/Displays/TaskDisplay'

enum CloeAnalysableDescriptions {
    SphericalGlass = 'Spherical glass',
}

const checkIfAnalysableDescription = (description: string) => {
    return Object.values(CloeAnalysableDescriptions).includes(description as CloeAnalysableDescriptions)
}

const CloeDataTable = ({ tasks }: { tasks: Task[] }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>#</Table.Cell>
                    <Table.Cell>{TranslateText('Tag-ID')}</Table.Cell>
                    <Table.Cell>{TranslateText('Description')}</Table.Cell>
                    <Table.Cell>{TranslateText('Latest Value')}</Table.Cell>
                    <Table.Cell>{TranslateText('Timestamp')}</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {tasks &&
                    tasks.map((task, index) => (
                        <Table.Row key={task.id}>
                            <Table.Cell>
                                <Chip>
                                    <Typography variant="body_short_bold">{index + 1}</Typography>
                                </Chip>
                            </Table.Cell>
                            <Table.Cell>
                                <TagIdDisplay task={task} />
                            </Table.Cell>
                            <Table.Cell>
                                <DescriptionDisplay task={task} />
                            </Table.Cell>
                            {task.inspection.analysisResult?.value ? (
                                <Table.Cell>
                                    <Typography>
                                        {Math.round(parseFloat(task.inspection.analysisResult?.value)) + '%'}
                                    </Typography>
                                </Table.Cell>
                            ) : (
                                <Table.Cell>
                                    <Typography>Analysis result not available</Typography>
                                </Table.Cell>
                            )}
                            {task.endTime && (
                                <Table.Cell>
                                    <Typography>{formatDateTime(task.endTime, 'dd.MM.yy - HH:mm')}</Typography>
                                </Table.Cell>
                            )}
                        </Table.Row>
                    ))}
            </Table.Body>
        </StyledTable>
    )
}

export const CloeDataViewPage = () => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { registerEvent, connectionReady } = useSignalRContext()
    const [cloeMissions, setCloeMissions] = useState<Mission[]>([])
    const [latestCloeMission, setLatestCloeMission] = useState<Mission>()
    const [timeRange, setTimeRange] = useState<'7days' | '1month'>('1month')
    const backendApi = useBackendApi()

    const fetchMissions = (): Promise<Mission[]> => {
        return backendApi
            .getMissionRuns({
                installationCode: installation.installationCode,
                orderBy: 'EndTime desc, Name',
                statuses: [MissionStatus.Successful, MissionStatus.PartiallySuccessful],
                nameSearch: 'Avlesning',
            })
            .then((missionRuns) => {
                const missions = missionRuns.content
                return missions
            })
            .catch(() => {
                return []
            })
    }

    useEffect(() => {
        fetchMissions().then((missions) => {
            setCloeMissions(missions)
        })
    }, [installation.installationCode])

    useEffect(() => {
        if (cloeMissions.length > 0) {
            setLatestCloeMission(cloeMissions[0])
        }
    }, [cloeMissions])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.analysisResultReady, () => {
                fetchMissions().then((missions) => {
                    setCloeMissions(missions)
                })
            })
        }
    }, [registerEvent, connectionReady])

    const plantCode = latestCloeMission?.inspectionArea.plantCode ?? undefined

    const sphericalGlassTaskMission = (mission: Mission): Mission => ({
        ...mission,
        tasks: mission.tasks
            .filter((task) => (task.description ? checkIfAnalysableDescription(task.description) : false))
            .map((task, index) => ({ ...task, taskOrder: index })),
    })

    const missionForTableAndMap = latestCloeMission ? sphericalGlassTaskMission(latestCloeMission) : undefined

    const timeRangeCutoff =
        timeRange === '7days' ? Date.now() - 7 * 24 * 60 * 60 * 1000 : Date.now() - 30 * 24 * 60 * 60 * 1000

    const recentSphericalGlassMissions = cloeMissions
        .filter((mission) => {
            const missionTimestamp = mission.endTime ?? mission.startTime ?? mission.creationTime
            if (!missionTimestamp) return false
            return new Date(missionTimestamp).getTime() >= timeRangeCutoff
        })
        .map(sphericalGlassTaskMission)

    const linePlotData: TimeseriesLinePlotData = recentSphericalGlassMissions.reduce<TimeseriesLinePlotData>(
        (accumulatedData, mission) => {
            const missionTimestamp = new Date(
                (mission.endTime ?? mission.startTime ?? mission.creationTime) as unknown as string
            )
            mission.tasks.forEach((task) => {
                const tagId = task.tagId
                const rawFillLevel = task.inspection?.analysisResult?.value
                const sampleTimestamp =
                    task.endTime || task.startTime
                        ? new Date((task.endTime ?? task.startTime) as unknown as string)
                        : missionTimestamp
                if (!tagId || !rawFillLevel || !sampleTimestamp) return
                if (!Object.hasOwn(accumulatedData, tagId)) {
                    accumulatedData[tagId] = []
                }
                accumulatedData[tagId].push({ time: sampleTimestamp, value: Number(rawFillLevel) })
            })
            return accumulatedData
        },
        {} as TimeseriesLinePlotData
    )

    return (
        <StyledPage>
            <StyledCardsWidth>
                <Typography variant="h2">Data View for Constant Level Oilers</Typography>
                <StyledTableAndMap>
                    {missionForTableAndMap && <CloeDataTable tasks={missionForTableAndMap.tasks} />}
                    {plantCode && missionForTableAndMap && (
                        <PlantMap plantCode={plantCode} floorId="0" mission={missionForTableAndMap} />
                    )}
                </StyledTableAndMap>
                {recentSphericalGlassMissions.length > 0 && (
                    <>
                        <Typography variant="h4">{TranslateText('Measured oil level')}</Typography>
                        <ButtonGroup>
                            <Button
                                variant={timeRange === '7days' ? 'contained' : 'outlined'}
                                onClick={() => setTimeRange('7days')}
                            >
                                {TranslateText('7 days')}
                            </Button>
                            <Button
                                variant={timeRange === '1month' ? 'contained' : 'outlined'}
                                onClick={() => setTimeRange('1month')}
                            >
                                {TranslateText('1 month')}
                            </Button>
                        </ButtonGroup>
                        <TimeseriesLinePlot data={linePlotData} yLabel={TranslateText('Fill [%]')} />
                    </>
                )}
            </StyledCardsWidth>
        </StyledPage>
    )
}
