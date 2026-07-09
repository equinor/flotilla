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
import { AnalysisOverviewSection, InspectionOverviewSection } from 'pages/InspectionReportPage/ImageOverview'
import {
    InspectionImageWithPlaceholder,
    PendingResultPlaceholder,
    TextAsImage,
} from 'pages/InspectionReportPage/InspectionReportImage'
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
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { AnalysisType } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import { useAssetContext } from 'components/Contexts/AssetContext'

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
    numberOfDaysOfData: number
    setNumberOfDaysOfData: (days: number) => void
    pageTitle: string
    plotTitle: string
    plotAriaLabel: string
    plotYLabel: string
    plotYMin: number
    plotYMax: number
}

const DataViewContent = ({
    inspectionData,
    numberOfDaysOfData,
    setNumberOfDaysOfData,
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

    const linePlotData = useMemo(() => {
        const plotData: TimeseriesLinePlotData = {}
        const inspectionLookup = new Map<string, InspectionData>()
        inspectionData.forEach((inspection) => {
            const tagId = inspection.tag
            const sampleTimestamp = inspection.createdAt

            if (inspection.value == null || inspection.value === '' || !sampleTimestamp) return
            if (selectedInspectionId && inspection.inspectionId !== selectedInspectionId) return
            if (!Object.hasOwn(plotData, tagId)) plotData[tagId] = []
            plotData[tagId].push({
                time: sampleTimestamp,
                value: parseFloat(inspection.value),
                inspectionId: inspection.inspectionId,
            })
            inspectionLookup.set(inspection.inspectionId, inspection)
        })
        return plotData
    }, [inspectionData, selectedInspectionId])

    const selectedInspection = inspectionData.find((i) => i.inspectionId === selectedInspectionId)

    const inspectionImageTitle = selectedInspection
        ? TranslateText('Selected inspection')
        : TranslateText('Latest inspection')
    const analysisImageTitle = selectedInspection
        ? TranslateText('Selected analysis result')
        : TranslateText('Latest analysis result')

    return (
        <StyledPage>
            <Typography variant="h2">{TranslateText(pageTitle)}</Typography>
            <WhiteBackgroundBand>
                <StyledTableAndMap>
                    <DataViewTable
                        inspectionData={inspectionData}
                        selectedInspectionId={selectedInspectionId}
                        onSelectInspection={(inspectionId) => setSelectedInspectionId(inspectionId)}
                    />
                    {plantCode ? (
                        <DataViewMapWrapper>
                            <InspectionsPlantMap
                                key={selectedInspectionId ?? 'all'}
                                plantCode={plantCode}
                                floorId="0"
                                inspections={inspectionData}
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
                <TimeRangeToggle role="group" aria-label={TranslateText(plotAriaLabel)}>
                    <TimeRangeToggleButton
                        variant={numberOfDaysOfData === 7 ? 'contained' : 'ghost'}
                        aria-pressed={numberOfDaysOfData === 7}
                        onClick={() => {
                            setNumberOfDaysOfData(7)
                            setSelectedInspectionId(undefined)
                        }}
                    >
                        {TranslateText('7 days')}
                    </TimeRangeToggleButton>
                    <TimeRangeToggleButton
                        variant={numberOfDaysOfData === 30 ? 'contained' : 'ghost'}
                        aria-pressed={numberOfDaysOfData === 30}
                        onClick={() => {
                            setNumberOfDaysOfData(30)
                            setSelectedInspectionId(undefined)
                        }}
                    >
                        {TranslateText('1 month')}
                    </TimeRangeToggleButton>
                </TimeRangeToggle>
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
            {!selectedInspectionId && (
                <WhiteBackgroundBand>
                    <InspectionOverviewSection inspectionData={inspectionData} />
                    <AnalysisOverviewSection inspectionData={inspectionData} />
                </WhiteBackgroundBand>
            )}
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
    const [numberOfDaysOfData, setNumberOfDaysOfData] = useState<number>(30)
    const [currentTime] = useState<Date>(new Date())
    const minDate = new Date(new Date().setDate(currentTime.getDate() - numberOfDaysOfData))

    const { data, isPending, isError } = useSaraListData(
        null,
        installation.installationCode,
        null,
        analysisType,
        minDate,
        null
    )

    if (isPending) {
        return <PendingResultPlaceholder isLargeImage={true} />
    } else if (isError || !data) {
        return <TextAsImage isLargeImage={true} text={'No inspection could be found'} />
    }

    // Fetches only the latest inspection data per tag
    const filteredInspectionData = data.reduce((accumulator: InspectionData[], item) => {
        const index = accumulator.findIndex((i) => i.tag === item.tag)
        if (index >= 0) accumulator[index] = item
        else accumulator.push(item)
        return accumulator
    }, [])

    return (
        <DataViewContent
            inspectionData={filteredInspectionData}
            numberOfDaysOfData={numberOfDaysOfData}
            setNumberOfDaysOfData={setNumberOfDaysOfData}
            pageTitle={pageTitle}
            plotTitle={plotTitle}
            plotAriaLabel={plotAriaLabel}
            plotYLabel={plotYLabel}
            plotYMin={plotYMin}
            plotYMax={plotYMax}
        />
    )
}
