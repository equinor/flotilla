import { useContext, useMemo, useState } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { ContentCard, PageBackground, PageContent } from 'components/Styles/StyledComponents'
import { InstallationContext } from 'contexts/InstallationContext'
import { useInspectionsContext } from 'contexts/InspectionsContext'
import { AnalysisType } from 'models/MissionDefinition'
import { PendingResultPlaceholder, TextAsImage } from 'pages/InspectionReportPage/InspectionReportImage'
import { useAssetContext } from 'contexts/AssetContext'
import { DataViewMapWrapper } from './DataViewComponents'
import { InspectionsPlantMap } from 'pages/MissionPage/MapPosition/PointillaMapView'
import { InspectionData } from 'models/InspectionRecord'
import {
    StyledImageCard,
    StyledImagesSection,
    StyledInspectionCards,
    StyledInspectionContent,
    StyledInspectionData,
    StyledInspectionImage,
    StyledInspectionOverviewSection,
} from 'pages/InspectionReportPage/InspectionStyles'
import { DatePicker, Typography } from '@equinor/eds-core-react'
import { formatDateTime } from 'utils/StringFormatting'
import { InspectionDialogView } from 'pages/InspectionReportPage/InspectionView'
import { tokens } from '@equinor/eds-tokens'
import { DataViewTimeRange } from './DataViewTimeRange'
import { AlertBanner } from 'components/Alerts/AlertsBanner'
import styled from 'styled-components'

const StyledDiv = styled.div`
    display: flex;
    width: 100%;
    align-items: center;
`

const AnalysisImageWithPlaceholder = ({ data }: { data: InspectionData }) => {
    if (data.visualizedSAS) {
        return <StyledInspectionImage src={data.visualizedSAS} />
    } else if (data.anonymizedSAS) {
        return <StyledInspectionImage src={data.anonymizedSAS} />
    } else {
        return <TextAsImage isLargeImage={false} text={'No data available'} />
    }
}

const get7DayWindow = (endDate: Date | null): DataViewTimeRange => {
    const minDate = new Date(endDate ?? new Date())
    minDate.setDate(minDate.getDate() - 7)
    minDate.setHours(0, 0, 0, 0)

    return { minDate, maxDate: endDate }
}

export const FencillaViewPage = () => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()

    const { useSaraListData } = useInspectionsContext()
    const [filterDates, setFilterDates] = useState<DataViewTimeRange>(() => get7DayWindow(new Date()))
    const [selectedInspectionIndex, setSelectedInspectionIndex] = useState<number | undefined>()

    const { data, isPending, isError } = useSaraListData(
        null,
        installation.installationCode,
        null,
        AnalysisType.Fencilla,
        filterDates.minDate,
        filterDates.maxDate
    )

    const mostRecentInspections = useMemo(() => {
        if (!data) return []
        const descriptionToInspectionMap = new Map<string, InspectionData>()
        data.forEach((inspection) => {
            if (!descriptionToInspectionMap.has(inspection.inspectionDescription)) {
                descriptionToInspectionMap.set(inspection.inspectionDescription, inspection)
            } else if (
                descriptionToInspectionMap.get(inspection.inspectionDescription)!.value == null &&
                inspection.value != null
            ) {
                descriptionToInspectionMap.set(inspection.inspectionDescription, inspection)
            }
        })
        return Array.from(descriptionToInspectionMap.values())
    }, [data])

    if (isPending) {
        return <PendingResultPlaceholder isLargeImage={true} />
    }

    const plantCode =
        installationInspectionAreas.find((i) => i.installationCode === installation.installationCode)?.plantCode ?? null

    const selectedInspection =
        selectedInspectionIndex !== undefined ? mostRecentInspections[selectedInspectionIndex] : undefined

    document.addEventListener('keyup', (event) => {
        if (selectedInspectionIndex === undefined) return
        if (event.target instanceof HTMLMediaElement) return
        if (event.code === 'ArrowLeft') {
            if (selectedInspectionIndex - 1 < 0) return
            setSelectedInspectionIndex(selectedInspectionIndex - 1)
        } else if (event.code === 'ArrowRight') {
            if (selectedInspectionIndex + 1 >= mostRecentInspections.length) return
            setSelectedInspectionIndex(selectedInspectionIndex + 1)
        }
    })

    const mapDisplay = plantCode ? (
        <ContentCard>
            <DataViewMapWrapper>
                <InspectionsPlantMap
                    plantCode={plantCode}
                    floorId="0"
                    inspections={mostRecentInspections}
                    onMarkerClick={(markerIndex: number) => setSelectedInspectionIndex(markerIndex)}
                />
            </DataViewMapWrapper>
        </ContentCard>
    ) : (
        <></>
    )

    const largeImageDialog = selectedInspection && selectedInspectionIndex !== undefined && (
        <InspectionDialogView
            inspectionData={selectedInspection}
            title={TranslateText('Inspection report for task') + ' ' + (selectedInspectionIndex + 1)}
            onClose={() => setSelectedInspectionIndex(undefined)}
        />
    )

    return (
        <PageBackground>
            <PageContent>
                <Typography variant="h4">{TranslateText('Data View for Perimeter Breach Detection')}</Typography>
                {isError && (
                    <AlertBanner
                        bannerAlert={{
                            message: TranslateText('Could not load data for the selected time range'),
                            severity: 'error',
                        }}
                        dismissAlert={() => {}}
                    />
                )}
                <StyledDiv>
                    <Typography variant="h6">{TranslateText('Show data until selected date') + ':'}</Typography>
                    <DatePicker
                        onChange={(newDate: Date | null) => setFilterDates(get7DayWindow(newDate))}
                        value={filterDates.maxDate}
                        maxValue={new Date()}
                    />
                </StyledDiv>
                <StyledInspectionOverviewSection>
                    <StyledImagesSection>
                        <StyledInspectionCards>
                            {mostRecentInspections.map((inspection, index) => (
                                <StyledImageCard
                                    key={inspection.inspectionId}
                                    onClick={() => setSelectedInspectionIndex(index)}
                                    style={{
                                        backgroundColor: inspection.warning
                                            ? tokens.colors.ui.background__danger.hex
                                            : 'white',
                                    }}
                                >
                                    <AnalysisImageWithPlaceholder data={inspection} />
                                    <StyledInspectionData>
                                        {inspection.tag && (
                                            <StyledInspectionContent>
                                                <Typography variant="caption">
                                                    {TranslateText('Index') + ' ' + (index + 1)}
                                                </Typography>
                                            </StyledInspectionContent>
                                        )}
                                        {inspection.createdAt && (
                                            <StyledInspectionContent>
                                                <Typography variant="caption">
                                                    {TranslateText('Timestamp') + ':'}
                                                </Typography>
                                                <Typography variant="body_short">
                                                    {formatDateTime(inspection.createdAt)}
                                                </Typography>
                                            </StyledInspectionContent>
                                        )}
                                    </StyledInspectionData>
                                </StyledImageCard>
                            ))}
                        </StyledInspectionCards>
                    </StyledImagesSection>
                </StyledInspectionOverviewSection>
                {mapDisplay}
                {largeImageDialog}
            </PageContent>
        </PageBackground>
    )
}
