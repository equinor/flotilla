import { Typography } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useContext, useEffect, useMemo, useState } from 'react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StyledPage, StyledTableAndMap } from 'components/Styles/StyledComponents'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Task } from 'models/Task'
import { TimeseriesLinePlot, TimeseriesLinePlotData } from 'components/Displays/TimeseriesLinePlot'
import { PlantMap } from 'pages/MissionPage/MapPosition/PointillaMapView'
import { AnalysisOverviewSection, InspectionOverviewSection } from 'pages/InspectionReportPage/ImageOverview'
import { InspectionImageWithPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { AnalysisResultDialogContent } from 'pages/MissionPage/AnalysisResultView'
import { InspectionDialogView } from 'pages/InspectionReportPage/InspectionView'
import { AnalysisResultDialogView } from 'pages/MissionPage/AnalysisResultView'
import { useSearchParams } from 'react-router-dom'
import { DataViewTable } from './DataViewTable'
import {
    DataViewChartArea,
    DataViewMapWrapper,
    StyledDataViewImageCard,
    StyledTopAlignedImagesSection,
    TimeRangeToggle,
    TimeRangeToggleButton,
    WhiteBackgroundBand,
} from './DataViewComponents'

export enum AnalysisTypes {
    Fencilla = 'fencilla',
    CLOE = 'cloe',
    ThermalReading = 'thermal-reading',
}

enum TimeRange {
    SevenDays = '7days',
    OneMonth = '1month',
}

const FETCH_WINDOW_DAYS = 30
const PAGE_SIZE = 200
const MS_PER_SECOND = 1000
const MS_PER_DAY = 24 * 60 * 60 * MS_PER_SECOND

interface DataViewProps {
    analysisType: AnalysisTypes
    taskFilter?: (task: Task) => boolean
    pageTitle: string
    plotTitle: string
    plotAriaLabel: string
    plotYLabel: string
    plotYMin: number
    plotYMax: number
}

export const DataView = ({
    analysisType,
    taskFilter,
    pageTitle,
    plotTitle,
    plotAriaLabel,
    plotYLabel,
    plotYMin,
    plotYMax,
}: DataViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { registerEvent, connectionReady } = useSignalRContext()
    const [missions, setMissions] = useState<Mission[]>([])
    const [timeRange, setTimeRange] = useState<TimeRange>(TimeRange.OneMonth)
    const [selectedTagId, setSelectedTagId] = useState<string | null>(null)
    const [selectedDataPoint, setSelectedDataPoint] = useState<{ tagId: string; timeMs: number } | null>(null)
    const backendApi = useBackendApi()
    const [searchParams] = useSearchParams()
    const inspectionId = searchParams.get('inspectionId') ?? undefined
    const analysisId = searchParams.get('analysisId') ?? undefined

    const fetchMissions = (): Promise<Mission[]> => {
        const minEndTime = Math.floor((Date.now() - FETCH_WINDOW_DAYS * MS_PER_DAY) / MS_PER_SECOND)
        return backendApi
            .getMissionRuns({
                installationCode: installation.installationCode,
                orderBy: 'EndTime desc, Name',
                statuses: [MissionStatus.Successful, MissionStatus.PartiallySuccessful],
                minEndTime: minEndTime,
                pageSize: PAGE_SIZE,
            })
            .then((missionRuns) => {
                const missionRunsContent = missionRuns.content
                return missionRunsContent
            })
            .catch(() => {
                return []
            })
    }

    const filterTaskMission = (mission: Mission): Mission => ({
        ...mission,
        tasks: mission.tasks
            .filter(
                (task) =>
                    task.inspection?.analysisResult?.analysisType === analysisType &&
                    (taskFilter ? taskFilter(task) : true)
            )
            .map((task, index) => ({ ...task, taskOrder: index })),
    })

    const filterMissions = (missionsToFilter: Mission[]) =>
        missionsToFilter.map(filterTaskMission).filter((m) => m.tasks.length > 0)

    useEffect(() => {
        fetchMissions().then((fetchedMissions) => {
            setMissions(filterMissions(fetchedMissions))
        })
    }, [installation.installationCode])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.analysisResultReady, () => {
                fetchMissions().then((fetchedMissions) => {
                    setMissions(filterMissions(fetchedMissions))
                })
            })
        }
    }, [registerEvent, connectionReady])

    const latestMission = missions[0]
    const plantCode = latestMission?.inspectionArea.plantCode

    const getTimeRangeCutoff = (range: TimeRange): number => {
        const millisecondsInADay = 24 * 60 * 60 * 1000
        const days = range === TimeRange.SevenDays ? 7 : 30
        return Date.now() - days * millisecondsInADay
    }

    const recentMissions = missions.filter((mission) => {
        const missionTimestamp = mission.endTime ?? mission.startTime ?? mission.creationTime
        if (!missionTimestamp) return false
        return new Date(missionTimestamp).getTime() >= getTimeRangeCutoff(timeRange)
    })

    const makeLookupKey = (tagId: string, timeMs: number) => `${tagId}|${timeMs}`
    const { linePlotData, dataPointTaskLookup } = useMemo(() => {
        const plotData: TimeseriesLinePlotData = {}
        const taskLookup = new Map<string, Task>()
        recentMissions.forEach((mission) => {
            const missionTimestamp = new Date(
                (mission.endTime ?? mission.startTime ?? mission.creationTime) as unknown as string
            )
            mission.tasks.forEach((task) => {
                const tagId = task.tagId
                const rawValue = task.inspection?.analysisResult?.value
                const sampleTimestamp =
                    task.endTime || task.startTime
                        ? new Date((task.endTime ?? task.startTime) as unknown as string)
                        : missionTimestamp
                if (!tagId || rawValue == null || rawValue === '') return
                if (Number.isNaN(sampleTimestamp.getTime())) return
                if (selectedTagId && tagId !== selectedTagId) return
                if (!Object.hasOwn(plotData, tagId)) plotData[tagId] = []
                plotData[tagId].push({ time: sampleTimestamp, value: Number(rawValue) })
                taskLookup.set(makeLookupKey(tagId, sampleTimestamp.getTime()), task)
            })
        })
        return { linePlotData: plotData, dataPointTaskLookup: taskLookup }
    }, [recentMissions, selectedTagId])

    const mapMission = (() => {
        if (!latestMission) return undefined
        if (!selectedTagId) return latestMission
        const selectedIndex = latestMission.tasks.findIndex((t) => t.tagId === selectedTagId)
        if (selectedIndex < 0) return latestMission
        const selectedMapTask = latestMission.tasks[selectedIndex]
        const paddedTasks: Task[] = Array.from({ length: selectedIndex + 1 }, (_, i) => ({
            ...selectedMapTask,
            id: `${selectedMapTask.id}__pad_${i}`,
        }))
        paddedTasks[selectedIndex] = selectedMapTask
        return { ...latestMission, tasks: paddedTasks }
    })()

    const selectedDataPointTask = selectedDataPoint
        ? dataPointTaskLookup.get(makeLookupKey(selectedDataPoint.tagId, selectedDataPoint.timeMs))
        : undefined
    const selectedTask =
        selectedDataPointTask ??
        (selectedTagId ? latestMission?.tasks.find((t) => t.tagId === selectedTagId) : undefined)
    const isSelectedTaskFromDataPoint = !!selectedDataPointTask
    const inspectionImageTitle = isSelectedTaskFromDataPoint
        ? TranslateText('Selected inspection')
        : TranslateText('Latest inspection')
    const analysisImageTitle = isSelectedTaskFromDataPoint
        ? TranslateText('Selected analysis result')
        : TranslateText('Latest analysis result')

    return (
        <StyledPage>
            <Typography variant="h2">{TranslateText(pageTitle)}</Typography>
            {(latestMission || (plantCode && mapMission)) && (
                <WhiteBackgroundBand>
                    <StyledTableAndMap>
                        {latestMission && (
                            <DataViewTable
                                tasks={latestMission.tasks}
                                selectedTagId={selectedTagId}
                                onSelectTag={(tagId) => {
                                    setSelectedTagId(tagId)
                                    setSelectedDataPoint(null)
                                }}
                            />
                        )}
                        {plantCode && mapMission && (
                            <DataViewMapWrapper>
                                <PlantMap
                                    key={selectedTagId ?? 'all'}
                                    plantCode={plantCode}
                                    floorId="0"
                                    mission={mapMission}
                                />
                            </DataViewMapWrapper>
                        )}
                    </StyledTableAndMap>
                </WhiteBackgroundBand>
            )}
            {selectedTask && selectedTask.inspection.isarInspectionId && (
                <StyledTopAlignedImagesSection>
                    <StyledDataViewImageCard>
                        <Typography variant="h4">{inspectionImageTitle}</Typography>
                        <InspectionImageWithPlaceholder task={selectedTask} isLargeImage={true} />
                    </StyledDataViewImageCard>
                    {selectedTask.inspection.analysisResult?.storageAccount && (
                        <StyledDataViewImageCard>
                            <Typography variant="h4">{analysisImageTitle}</Typography>
                            <AnalysisResultDialogContent currentTask={selectedTask} />
                        </StyledDataViewImageCard>
                    )}
                </StyledTopAlignedImagesSection>
            )}
            {missions.length > 0 && (
                <DataViewChartArea>
                    <Typography variant="h3">{TranslateText(plotTitle)}</Typography>
                    <TimeRangeToggle role="group" aria-label={TranslateText(plotAriaLabel)}>
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
                    {recentMissions.length > 0 ? (
                        <TimeseriesLinePlot
                            data={linePlotData}
                            yLabel={TranslateText(plotYLabel)}
                            ymin={plotYMin}
                            ymax={plotYMax}
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
                </DataViewChartArea>
            )}
            {latestMission && !selectedDataPoint && (
                <WhiteBackgroundBand>
                    <InspectionOverviewSection tasks={latestMission.tasks} />
                    <AnalysisOverviewSection tasks={latestMission.tasks} />
                </WhiteBackgroundBand>
            )}
            {latestMission && inspectionId && !selectedDataPoint && (
                <InspectionDialogView selectedInspectionId={inspectionId} tasks={latestMission.tasks} />
            )}
            {latestMission && analysisId && !selectedDataPoint && (
                <AnalysisResultDialogView selectedAnalysisId={analysisId} tasks={latestMission.tasks} />
            )}
        </StyledPage>
    )
}
