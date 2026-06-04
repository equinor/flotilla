import { Button, Chip, Table, Typography } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useContext, useEffect, useMemo, useState } from 'react'
import styled from 'styled-components'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    StyledCardsWidth,
    StyledPage,
    StyledTable,
    StyledTableAndMap,
    StyledTableCell,
} from 'components/Styles/StyledComponents'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Task } from 'models/Task'
import { formatDateTime } from 'utils/StringFormatting'
import { TimeseriesLinePlot, TimeseriesLinePlotData } from 'components/Displays/TimeseriesLinePlot'
import { DescriptionDisplay, TagIdDisplay } from 'components/Displays/TaskDisplay'
import { PlantMap } from 'pages/MissionPage/MapPosition/PointillaMapView'
import { StyledImagesSection } from 'pages/InspectionReportPage/InspectionStyles'
import { InspectionImageWithPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { AnalysisResultDialogContent } from 'pages/MissionPage/AnalysisResultView'

const CloeMapWrapper = styled.div`
    .leaflet-tooltip.circleLabel {
        background-color: ${tokens.colors.ui.background__medium.hex} !important;
        padding: 0 4px !important;
        border-radius: 2px !important;
    }
`
const CloeTableWrapper = styled.div`
    align-self: flex-start;
`
const TimeRangeToggle = styled.div`
    display: inline-flex;
    align-self: flex-start;
    gap: 4px;
    padding: 4px;
    border-radius: 6px;
    background-color: ${tokens.colors.ui.background__light.hex};
    box-shadow: inset 0 0 0 1px ${tokens.colors.ui.background__medium.hex};
`
const TimeRangeToggleButton = styled(Button)`
    border-radius: 4px;
`
const StyledTopAlignedImagesSection = styled(StyledImagesSection)`
    align-items: flex-start;
`
const StyledCloeImageCard = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
    max-width: 480px;
`

enum TimeRange {
    SevenDays = '7days',
    OneMonth = '1month',
}

const CLOE_FETCH_WINDOW_DAYS = 30
const CLOE_PAGE_SIZE = 200
const MS_PER_SECOND = 1000
const MS_PER_DAY = 24 * 60 * 60 * MS_PER_SECOND

enum CloeMissionNames {
    Avlesning = 'Avlesning',
}

enum CloeAnalysableDescriptions {
    SphericalGlass = 'Spherical glass',
}

const checkIfAnalysableDescription = (description: string) => {
    return Object.values(CloeAnalysableDescriptions).includes(description as CloeAnalysableDescriptions)
}

const CloeDataTable = ({
    tasks,
    selectedTagId,
    onSelectTag,
}: {
    tasks: Task[]
    selectedTagId: string | null
    onSelectTag: (tagId: string | null) => void
}) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <StyledTableCell>#</StyledTableCell>
                    <StyledTableCell>{TranslateText('Tag-ID')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Description')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Latest Value')}</StyledTableCell>
                    <StyledTableCell>{TranslateText('Timestamp')}</StyledTableCell>
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {tasks &&
                    tasks.map((task, index) => {
                        const isSelected = !!task.tagId && task.tagId === selectedTagId
                        const taskHasWarning = !!task.inspection.analysisResult?.warning
                        const backgroundColor = isSelected
                            ? tokens.colors.interactive.primary__selected_highlight.rgba
                            : taskHasWarning
                              ? tokens.colors.interactive.danger__highlight.rgba
                              : undefined

                        return (
                            <Table.Row
                                key={task.id}
                                onClick={() => {
                                    if (!task.tagId) return
                                    onSelectTag(isSelected ? null : task.tagId)
                                }}
                                style={{
                                    cursor: task.tagId ? 'pointer' : 'default',
                                    backgroundColor,
                                }}
                            >
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
                                        <Typography>{TranslateText('Analysis result not available')}</Typography>
                                    </Table.Cell>
                                )}
                                {task.endTime && (
                                    <Table.Cell>
                                        <Typography>{formatDateTime(task.endTime, 'dd.MM.yy - HH:mm')}</Typography>
                                    </Table.Cell>
                                )}
                            </Table.Row>
                        )
                    })}
            </Table.Body>
        </StyledTable>
    )
}

export const CloeDataViewPage = () => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { registerEvent, connectionReady } = useSignalRContext()
    const [cloeMissions, setCloeMissions] = useState<Mission[]>([])
    const [timeRange, setTimeRange] = useState<TimeRange>(TimeRange.OneMonth)
    const [selectedTagId, setSelectedTagId] = useState<string | null>(null)
    const [selectedDataPoint, setSelectedDataPoint] = useState<{ tagId: string; timeMs: number } | null>(null)
    const backendApi = useBackendApi()

    const fetchMissions = (): Promise<Mission[]> => {
        const minEndTime = Math.floor((Date.now() - CLOE_FETCH_WINDOW_DAYS * MS_PER_DAY) / MS_PER_SECOND)
        return backendApi
            .getMissionRuns({
                installationCode: installation.installationCode,
                orderBy: 'EndTime desc, Name',
                statuses: [MissionStatus.Successful, MissionStatus.PartiallySuccessful],
                nameSearch: CloeMissionNames.Avlesning,
                minEndTime: minEndTime,
                pageSize: CLOE_PAGE_SIZE,
            })
            .then((missionRuns) => {
                const missions = missionRuns.content
                return missions
            })
            .catch(() => {
                return []
            })
    }

    const sphericalGlassTaskMission = (mission: Mission): Mission => ({
        ...mission,
        tasks: mission.tasks
            .filter((task) => (task.description ? checkIfAnalysableDescription(task.description) : false))
            .map((task, index) => ({ ...task, taskOrder: index })),
    })

    const filterCloeMissions = (missions: Mission[]) =>
        missions.map(sphericalGlassTaskMission).filter((m) => m.tasks.length > 0)

    useEffect(() => {
        fetchMissions().then((missions) => {
            setCloeMissions(filterCloeMissions(missions))
        })
    }, [installation.installationCode])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.analysisResultReady, () => {
                fetchMissions().then((missions) => {
                    setCloeMissions(filterCloeMissions(missions))
                })
            })
        }
    }, [registerEvent, connectionReady])

    const latestCloeMission = cloeMissions[0]
    const plantCode = latestCloeMission?.inspectionArea.plantCode

    const getTimeRangeCutoff = (range: TimeRange): number => {
        const millisecondsInADay = 24 * 60 * 60 * 1000
        const days = range === TimeRange.SevenDays ? 7 : 30
        return Date.now() - days * millisecondsInADay
    }

    const recentSphericalGlassMissions = cloeMissions.filter((mission) => {
        const missionTimestamp = mission.endTime ?? mission.startTime ?? mission.creationTime
        if (!missionTimestamp) return false
        return new Date(missionTimestamp).getTime() >= getTimeRangeCutoff(timeRange)
    })

    const makeLookupKey = (tagId: string, timeMs: number) => `${tagId}|${timeMs}`
    const { linePlotData, dataPointTaskLookup } = useMemo(() => {
        const plotData: TimeseriesLinePlotData = {}
        const taskLookup = new Map<string, Task>()
        recentSphericalGlassMissions.forEach((mission) => {
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
                if (selectedTagId && tagId !== selectedTagId) return
                if (!Object.hasOwn(plotData, tagId)) plotData[tagId] = []
                plotData[tagId].push({ time: sampleTimestamp, value: Number(rawFillLevel) })
                taskLookup.set(makeLookupKey(tagId, sampleTimestamp.getTime()), task)
            })
        })
        return { linePlotData: plotData, dataPointTaskLookup: taskLookup }
    }, [recentSphericalGlassMissions, selectedTagId])

    const mapMission = (() => {
        if (!latestCloeMission) return undefined
        if (!selectedTagId) return latestCloeMission
        const selectedIndex = latestCloeMission.tasks.findIndex((t) => t.tagId === selectedTagId)
        if (selectedIndex < 0) return latestCloeMission
        const selectedTask = latestCloeMission.tasks[selectedIndex]
        const paddedTasks: Task[] = Array.from({ length: selectedIndex + 1 }, (_, i) => ({
            ...selectedTask,
            id: `${selectedTask.id}__pad_${i}`,
        }))
        paddedTasks[selectedIndex] = selectedTask
        return { ...latestCloeMission, tasks: paddedTasks }
    })()

    const selectedDataPointTask = selectedDataPoint
        ? dataPointTaskLookup.get(makeLookupKey(selectedDataPoint.tagId, selectedDataPoint.timeMs))
        : undefined
    const selectedTask =
        selectedDataPointTask ??
        (selectedTagId ? latestCloeMission?.tasks.find((t) => t.tagId === selectedTagId) : undefined)
    const isSelectedTaskFromDataPoint = !!selectedDataPointTask
    const inspectionImageTitle = isSelectedTaskFromDataPoint
        ? TranslateText('Selected inspection')
        : TranslateText('Latest inspection')
    const analysisImageTitle = isSelectedTaskFromDataPoint
        ? TranslateText('Selected analysis result')
        : TranslateText('Latest analysis result')

    return (
        <StyledPage>
            <StyledCardsWidth>
                <Typography variant="h2">{TranslateText('Data View for Constant Level Oilers')}</Typography>
                <StyledTableAndMap>
                    {latestCloeMission && (
                        <CloeTableWrapper>
                            <CloeDataTable
                                tasks={latestCloeMission.tasks}
                                selectedTagId={selectedTagId}
                                onSelectTag={(tagId) => {
                                    setSelectedTagId(tagId)
                                    setSelectedDataPoint(null)
                                }}
                            />
                        </CloeTableWrapper>
                    )}
                    {plantCode && mapMission && (
                        <CloeMapWrapper>
                            <PlantMap
                                key={selectedTagId ?? 'all'}
                                plantCode={plantCode}
                                floorId="0"
                                mission={mapMission}
                            />
                        </CloeMapWrapper>
                    )}
                </StyledTableAndMap>
                {selectedTask && selectedTask.inspection.isarInspectionId && (
                    <StyledTopAlignedImagesSection>
                        <StyledCloeImageCard>
                            <Typography variant="h4">{inspectionImageTitle}</Typography>
                            <InspectionImageWithPlaceholder task={selectedTask} isLargeImage={true} />
                        </StyledCloeImageCard>
                        {selectedTask.inspection.analysisResult?.storageAccount && (
                            <StyledCloeImageCard>
                                <Typography variant="h4">{analysisImageTitle}</Typography>
                                <AnalysisResultDialogContent currentTask={selectedTask} />
                            </StyledCloeImageCard>
                        )}
                    </StyledTopAlignedImagesSection>
                )}
                {cloeMissions.length > 0 && (
                    <>
                        <Typography variant="h3">{TranslateText('Estimated oil level')}</Typography>
                        <TimeRangeToggle role="group" aria-label={TranslateText('Measured oil level')}>
                            <TimeRangeToggleButton
                                variant={timeRange === TimeRange.SevenDays ? 'contained' : 'ghost'}
                                aria-pressed={timeRange === TimeRange.SevenDays}
                                onClick={() => {
                                    setTimeRange(TimeRange.SevenDays)
                                    setSelectedDataPoint(null)
                                }}
                            >
                                {TranslateText('7 days')}
                            </TimeRangeToggleButton>
                            <TimeRangeToggleButton
                                variant={timeRange === TimeRange.OneMonth ? 'contained' : 'ghost'}
                                aria-pressed={timeRange === TimeRange.OneMonth}
                                onClick={() => {
                                    setTimeRange(TimeRange.OneMonth)
                                    setSelectedDataPoint(null)
                                }}
                            >
                                {TranslateText('1 month')}
                            </TimeRangeToggleButton>
                        </TimeRangeToggle>
                        {recentSphericalGlassMissions.length > 0 ? (
                            <TimeseriesLinePlot
                                data={linePlotData}
                                yLabel={TranslateText('Fill [%]')}
                                ymin={0}
                                ymax={100}
                                selectedPoint={
                                    selectedDataPoint
                                        ? {
                                              id: selectedDataPoint.tagId,
                                              time: new Date(selectedDataPoint.timeMs),
                                          }
                                        : undefined
                                }
                                onPointClick={({ id, time }) => {
                                    const timeMs = time.getTime()
                                    setSelectedDataPoint((current) =>
                                        current && current.tagId === id && current.timeMs === timeMs
                                            ? null
                                            : { tagId: id, timeMs }
                                    )
                                    setSelectedTagId(id)
                                }}
                            />
                        ) : (
                            <Typography>{TranslateText('No data available in the selected time range')}</Typography>
                        )}
                    </>
                )}
            </StyledCardsWidth>
        </StyledPage>
    )
}
