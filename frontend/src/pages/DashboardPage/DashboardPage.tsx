import { Typography } from '@equinor/eds-core-react'
import { useContext, useMemo } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { PageContent, ContentCard, PageBackground } from 'components/Styles/StyledComponents'
import { InstallationContext } from 'contexts/InstallationContext'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { DataViewMapWrapper } from 'pages/DataViewPage/DataViewComponents'
import { useInspectionsContext } from 'contexts/InspectionsContext'
import { AnalysisType } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import { createPresetTimeRange } from 'pages/DataViewPage/DataViewTimeRange'
import { SaraAlertCard } from './AlertComponent'
import { useAssetContext } from 'contexts/AssetContext'
import { InspectionsPlantMap } from 'pages/MissionPage/MapPosition/PointillaMapView'
import styled from 'styled-components'
import { MissionControlCard } from 'pages/MissionControlPage'
import { NextAutoScheduleMissionView } from 'pages/FrontPage/AutoScheduleSection/NextAutoScheduleMissionView'

interface DataViewContentProps {
    inspectionData: InspectionData[]
}

const DashboardColumns = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
`
const DashboardColumn = styled.div`
    display: flex;
    flex-direction: column;
    flex: auto;
    min-width: 0;
    gap: 16px;
`

const DashboardContent = ({ inspectionData }: DataViewContentProps) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()
    const { enabledRobots } = useAssetContext()

    const plantCode =
        installationInspectionAreas.find((i) => i.installationCode === installation.installationCode)?.plantCode ?? null

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
        <>
            <Typography variant="h2">{`${installation.name} - ${TranslateText('Dashboard')}`}</Typography>
            <DashboardColumns>
                <DashboardColumn>
                    {uniqueTagInspectionData
                        .filter((i) => i.warning)
                        .map((i) => (
                            <SaraAlertCard
                                key={i.analysisId}
                                analysisType={i.analysisType!}
                                tag={i.tag!}
                                createdAt={i.createdAt!}
                                value={i.value!}
                                unit={i.unit!}
                                warning={i.warning!}
                            />
                        ))}
                </DashboardColumn>
                <DashboardColumn>
                    {plantCode && (
                        <ContentCard>
                            <DataViewMapWrapper>
                                <InspectionsPlantMap
                                    key={'all'}
                                    plantCode={plantCode}
                                    floorId="0"
                                    inspections={uniqueTagInspectionData}
                                />
                            </DataViewMapWrapper>
                        </ContentCard>
                    )}
                    {enabledRobots.map((robot) => (
                        <MissionControlCard key={robot.id} robot={robot} />
                    ))}
                    <NextAutoScheduleMissionView />
                </DashboardColumn>
            </DashboardColumns>
        </>
    )
}

export const DashboardPage = () => {
    const { installation } = useContext(InstallationContext)
    const { useSaraListData } = useInspectionsContext()

    const timeRangeSelection = useMemo(() => {
        return { mode: 30, range: createPresetTimeRange(30) }
    }, [])

    const { data, isPending } = useSaraListData(
        null,
        installation.installationCode,
        null,
        AnalysisType.CLOE,
        timeRangeSelection.range.minDate,
        timeRangeSelection.range.maxDate
    )

    // Keep the page mounted on error, or the time range selector goes with it.
    return (
        <PageBackground>
            <PageContent>
                {isPending ? (
                    <PendingResultPlaceholder isLargeImage={true} />
                ) : (
                    <DashboardContent inspectionData={data ?? []} />
                )}
            </PageContent>
        </PageBackground>
    )
}
