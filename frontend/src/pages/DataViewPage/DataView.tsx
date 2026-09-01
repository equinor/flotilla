import { Typography } from '@equinor/eds-core-react'
import { useContext, useMemo, useState } from 'react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StyledPage, StyledTableAndMap } from 'components/Styles/StyledComponents'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import {
    TimeseriesLinePlot,
    TimeseriesLinePlotData,
    TimeseriesLinePlotDataPoint,
} from 'components/Displays/TimeseriesLinePlot'
import { InspectionsPlantMap } from 'pages/MissionPage/MapPosition/PointillaMapView'
import {
    InspectionImageWithPlaceholder,
    PendingResultPlaceholder,
    TextAsImage,
} from 'pages/InspectionReportPage/InspectionReportImage'
import { AnalysisResultDialogContent } from 'pages/MissionPage/AnalysisResultView'
import { InspectionDialogView } from 'pages/InspectionReportPage/InspectionView'
import { AnalysisResultDialogView } from 'pages/MissionPage/AnalysisResultView'
import { useSearchParams } from 'react-router'
import { DataViewTable } from './DataViewTable'
import {
    DataViewChartArea,
    DataViewMapWrapper,
    StyledDataViewImageCard,
    StyledTopAlignedImagesSection,
    WhiteBackgroundBand,
} from './DataViewComponents'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { AnalysisType } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { saraAnalysisTypeToEnum } from 'models/SaraAnalysisTypeMapping'
import { DataViewTimeRangeSelector } from './DataViewTimeRangeSelector'
import { createPresetTimeRange, DataViewTimeRange, TimeRangeMode } from './DataViewTimeRange'

interface DataViewProps {
    analysisType: AnalysisType
    pageTitle: string
    plotTitle: string
    plotAriaLabel: string
    plotYLabel: string
    plotYMin: number
    plotYMax: number
}

interface DataViewContentProps {
    inspectionData: InspectionData[]
    activeTimeRangeMode: TimeRangeMode
    activeTimeRange: DataViewTimeRange
    onApplyTimeRange: (mode: TimeRangeMode, range: DataViewTimeRange) => void
    pageTitle: string
    plotTitle: string
    plotAriaLabel: string
    plotYLabel: string
    plotYMin: number
    plotYMax: number
}

const DataViewContent = ({
    inspectionData,
    activeTimeRangeMode,
    activeTimeRange,
    onApplyTimeRange,
    pageTitle,
    plotTitle,
    plotAriaLabel,
    plotYLabel,
    plotYMin,
    plotYMax,
}: DataViewContentProps) => {
    const { TranslateText } = useLanguageContext()
    const [selectedInspectionId, setSelectedInspectionId] = useState<string | undefined>(undefined)
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()
    const [searchParams] = useSearchParams()
    const inspectionId = searchParams.get('inspectionId') ?? undefined
    const analysisId = searchParams.get('analysisId') ?? undefined

    const plantCode =
        installationInspectionAreas.find((i) => i.installationCode === installation.installationCode)?.plantCode ?? null
    const selectedInspection = useMemo(() => {
        return inspectionData.find((i) => i.inspectionId === selectedInspectionId)
    }, [inspectionData, selectedInspectionId])

    const linePlotData = useMemo(() => {
        const plotData: TimeseriesLinePlotData = {}
        inspectionData.forEach((inspection) => {
            const tagId = inspection.tag
            const sampleTimestamp = inspection.createdAt

            if (inspection.value == null || inspection.value === '' || !sampleTimestamp) return
            if (selectedInspection && tagId !== selectedInspection.tag) return
            if (!Object.hasOwn(plotData, tagId)) plotData[tagId] = []

            const value: number =
                saraAnalysisTypeToEnum(inspection.analysisType) === AnalysisType.CLOE
                    ? parseFloat(inspection.value) * 100
                    : parseFloat(inspection.value)
            plotData[tagId].push({
                time: sampleTimestamp,
                value: value,
                inspectionId: inspection.inspectionId,
            })
        })
        return plotData
    }, [inspectionData, selectedInspection])

    const inspectionImageTitle = selectedInspection
        ? TranslateText('Selected inspection')
        : TranslateText('Latest inspection')
    const analysisImageTitle = selectedInspection
        ? TranslateText('Selected analysis result')
        : TranslateText('Latest analysis result')

    // The table shall only show one line per tag
    // Assumes it is already sorted
    const uniqueTagInspectionData = useMemo(() => {
        const tagToInspectionMap = new Map<string, InspectionData>()
        inspectionData.forEach((inspection) => {
            if (!tagToInspectionMap.has(inspection.tag)) {
                tagToInspectionMap.set(inspection.tag, inspection)
            } else if (tagToInspectionMap.get(inspection.tag)!.value == null && inspection.value != null) {
                tagToInspectionMap.set(inspection.tag, inspection)
            }
        })
        return Array.from(tagToInspectionMap.values())
    }, [inspectionData])

    return (
        <StyledPage>
            <Typography variant="h2">{TranslateText(pageTitle)}</Typography>
            <WhiteBackgroundBand>
                <StyledTableAndMap>
                    <DataViewTable
                        uniqueTagInspectionData={uniqueTagInspectionData}
                        selectedInspectionId={selectedInspectionId}
                        onSelectInspection={(inspectionId) => setSelectedInspectionId(inspectionId)}
                    />
                    {plantCode ? (
                        <DataViewMapWrapper>
                            <InspectionsPlantMap
                                key={selectedInspectionId ?? 'all'}
                                plantCode={plantCode}
                                floorId="0"
                                inspections={uniqueTagInspectionData}
                            />
                        </DataViewMapWrapper>
                    ) : (
                        <></>
                    )}
                </StyledTableAndMap>
            </WhiteBackgroundBand>
            {selectedInspection && (
                <StyledTopAlignedImagesSection>
                    <StyledDataViewImageCard>
                        <Typography variant="h4">{inspectionImageTitle}</Typography>
                        <InspectionImageWithPlaceholder inspection={selectedInspection} isLargeImage={true} />
                    </StyledDataViewImageCard>
                    <StyledDataViewImageCard>
                        <Typography variant="h4">{analysisImageTitle}</Typography>
                        <AnalysisResultDialogContent inspection={selectedInspection} />
                    </StyledDataViewImageCard>
                </StyledTopAlignedImagesSection>
            )}
            <DataViewChartArea>
                <Typography variant="h3">{TranslateText(plotTitle)}</Typography>
                <DataViewTimeRangeSelector
                    ariaLabel={TranslateText(plotAriaLabel)}
                    activeMode={activeTimeRangeMode}
                    activeRange={activeTimeRange}
                    onApplyRange={(mode, range) => {
                        onApplyTimeRange(mode, range)
                        setSelectedInspectionId(undefined)
                    }}
                />
                {Object.keys(linePlotData).length > 0 ? (
                    <TimeseriesLinePlot
                        data={linePlotData}
                        yLabel={TranslateText(plotYLabel)}
                        ymin={plotYMin}
                        ymax={plotYMax}
                        selectedInspectionId={selectedInspectionId}
                        onPointClick={(point: TimeseriesLinePlotDataPoint) => {
                            setSelectedInspectionId((current) =>
                                current && current === point.inspectionId ? undefined : point.inspectionId
                            )
                        }}
                    />
                ) : (
                    <Typography>{TranslateText('No data available in the selected time range')}</Typography>
                )}
            </DataViewChartArea>
            {inspectionId && !selectedInspectionId && (
                <InspectionDialogView selectedInspectionId={inspectionId} inspectionData={inspectionData} />
            )}
            {analysisId && !selectedInspectionId && (
                <AnalysisResultDialogView selectedInspectionId={analysisId} inspectionData={inspectionData} />
            )}
        </StyledPage>
    )
}

export const DataView = ({
    analysisType,
    pageTitle,
    plotTitle,
    plotAriaLabel,
    plotYLabel,
    plotYMin,
    plotYMax,
}: DataViewProps) => {
    const { installation } = useContext(InstallationContext)
    const { useSaraListData } = useInspectionsContext()
    const [timeRangeSelection, setTimeRangeSelection] = useState<{
        mode: TimeRangeMode
        range: DataViewTimeRange
    }>(() => ({ mode: 30, range: createPresetTimeRange(30) }))

    const { data, isPending, isError } = useSaraListData(
        null,
        installation.installationCode,
        null,
        analysisType,
        timeRangeSelection.range.minDate,
        timeRangeSelection.range.maxDate
    )

    if (isPending) {
        return <PendingResultPlaceholder isLargeImage={true} />
    } else if (isError || !data) {
        return <TextAsImage isLargeImage={true} text={'No inspection could be found'} />
    }

    return (
        <DataViewContent
            inspectionData={data}
            activeTimeRangeMode={timeRangeSelection.mode}
            activeTimeRange={timeRangeSelection.range}
            onApplyTimeRange={(mode, range) => setTimeRangeSelection({ mode, range })}
            pageTitle={pageTitle}
            plotTitle={plotTitle}
            plotAriaLabel={plotAriaLabel}
            plotYLabel={plotYLabel}
            plotYMin={plotYMin}
            plotYMax={plotYMax}
        />
    )
}
