import { useContext, useMemo } from 'react'
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
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'
import { useSearchParams } from 'react-router'

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

const useFilterDate = () => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()

    const switchSelectedMaxDate = (maxDate: Date | null | undefined) => {
        setSearchParams(
            (prev) => {
                if (maxDate) prev.set('maxDate', maxDate.toDateString())
                else prev.delete('maxDate')
                return prev
            },
            { replace: true }
        )
    }

    return { switchSelectedMaxDate }
}

export const FencillaViewPageRouter = () => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()
    const inspectionId = searchParams.get('inspectionId') ?? undefined
    const maxDateString = searchParams.get('maxDate') ?? undefined

    const maxDate = maxDateString ? new Date(maxDateString) : undefined

    return <FencillaViewPage lookupInspectionId={inspectionId} maxDate={maxDate} />
}

const FencillaViewPage = ({
    lookupInspectionId,
    maxDate = new Date(),
}: {
    lookupInspectionId: string | undefined
    maxDate: Date | undefined
}) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()
    const { useSaraListData, useSaraData } = useInspectionsContext()
    const { switchSelectedMaxDate } = useFilterDate()
    const { switchSelectedInspectionId } = useInspectionId()

    const filterDates = get7DayWindow(maxDate)

    const plantCode =
        installationInspectionAreas.find((i) => i.installationCode === installation.installationCode)?.plantCode ?? null

    const { data, isPending, isError } = useSaraListData(
        null,
        installation.installationCode,
        null,
        AnalysisType.Fencilla,
        filterDates.minDate,
        filterDates.maxDate
    )

    const singleInspectionData = useSaraData(lookupInspectionId ?? '')

    const mostRecentInspections = useMemo(() => {
        const descriptionToInspectionMap = new Map<string, InspectionData>()
        const listData = data ?? []
        const lookupInspectionList = singleInspectionData.data ? [singleInspectionData.data] : []
        listData.concat(lookupInspectionList).forEach((inspection) => {
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
    }, [data, singleInspectionData.data])

    const selectedInspectionIndex = mostRecentInspections.findIndex((i) => i.inspectionId === lookupInspectionId)
    const selectedInspection =
        selectedInspectionIndex !== null && selectedInspectionIndex !== undefined
            ? mostRecentInspections[selectedInspectionIndex]
            : undefined

    document.addEventListener('keyup', (event) => {
        if (selectedInspectionIndex === undefined) return
        if (event.target instanceof HTMLMediaElement) return
        if (event.code === 'ArrowLeft') {
            if (selectedInspectionIndex - 1 < 0) return
            switchSelectedInspectionId(mostRecentInspections[selectedInspectionIndex - 1].inspectionId)
        } else if (event.code === 'ArrowRight') {
            if (selectedInspectionIndex + 1 >= mostRecentInspections.length) return
            switchSelectedInspectionId(mostRecentInspections[selectedInspectionIndex + 1].inspectionId)
        }
    })

    if (isPending) return <PendingResultPlaceholder isLargeImage={true} />

    const mapDisplay = plantCode ? (
        <ContentCard>
            <DataViewMapWrapper>
                <InspectionsPlantMap
                    plantCode={plantCode}
                    floorId="0"
                    inspections={mostRecentInspections}
                    onMarkerClick={(markerIndex: number) =>
                        switchSelectedInspectionId(mostRecentInspections[markerIndex].inspectionId)
                    }
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
            onClose={() => switchSelectedInspectionId(undefined)}
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
                        onChange={(newDate: Date | null) => switchSelectedMaxDate(newDate)}
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
                                    onClick={() =>
                                        switchSelectedInspectionId(mostRecentInspections[index].inspectionId)
                                    }
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
