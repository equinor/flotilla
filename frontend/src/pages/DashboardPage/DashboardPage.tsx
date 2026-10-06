import { useContext, useMemo } from 'react'
import { tokens } from '@equinor/eds-tokens'
import { ContentCard, PageBackground } from 'components/Styles/StyledComponents'
import { InstallationContext } from 'contexts/InstallationContext'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { useInspectionsContext } from 'contexts/InspectionsContext'
import { AnalysisType } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import { createPresetTimeRange } from 'pages/DataViewPage/DataViewTimeRange'
import { DashboardAlertPanel } from './DashboardAlertPanel'
import { DashboardRobotStatusCard } from './DashboardRobotStatusCard'
import { DashboardScheduledMissionsView } from './DashboardScheduledMissionsView'
import { useAssetContext } from 'contexts/AssetContext'
import { DashboardInspectionsPlantMap } from './DashboardInspectionsPlantMap'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'

interface DataViewContentProps {
    inspectionData: InspectionData[]
}

const DashboardZoomWrapper = styled.div`
    display: flex;
    flex-direction: column;
    flex: 1;
    min-height: 0;
    --dashboard-zoom: clamp(1, calc(100vw / 2195px), 3);
    zoom: var(--dashboard-zoom);
`

const DashboardPageContent = styled.div`
    width: 100%;
    min-width: 0;
    padding-inline: var(--eds-page-space-horizontal);
    display: flex;
    flex-direction: column;
    gap: 2rem;
`

const DashboardColumns = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
`
const DashboardColumn = styled.div`
    display: flex;
    flex-direction: column;
    flex: 1;
    min-width: 0;
    min-height: 0;
    height: calc(90vh / var(--dashboard-zoom));
    overflow-y: auto;
    gap: 16px;
`

const MapCard = styled(ContentCard)`
    flex: 1;
    min-height: 0;
    overflow: hidden;
`

const RobotList = styled(ContentCard)`
    padding-block: ${tokens.spacings.comfortable.xx_small};
    max-height: calc(40vh / var(--dashboard-zoom));
    overflow-y: auto;
`

const DashboardContent = ({ inspectionData }: DataViewContentProps) => {
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()
    const { enabledRobots } = useAssetContext()

    const plantCode =
        installationInspectionAreas.find((i) => i.installationCode === installation.installationCode)?.plantCode ?? null

    const alerts = useMemo(() => {
        const tagToInspectionMap = new Map<string, InspectionData>()
        inspectionData.forEach((inspection) => {
            if (!tagToInspectionMap.has(inspection.tag)) {
                tagToInspectionMap.set(inspection.tag, inspection)
            } else if (tagToInspectionMap.get(inspection.tag)!.value == null && inspection.value != null) {
                tagToInspectionMap.set(inspection.tag, inspection)
            }
        })
        return Array.from(tagToInspectionMap.values()).filter((i) => i.warning)
    }, [inspectionData])

    return (
        <DashboardColumns>
            <DashboardColumn>
                <DashboardAlertPanel alerts={alerts} />
            </DashboardColumn>
            <DashboardColumn>
                {plantCode && (
                    <MapCard>
                        <DashboardInspectionsPlantMap
                            key={'all'}
                            plantCode={plantCode}
                            floorId="0"
                            inspections={alerts}
                        />
                    </MapCard>
                )}
                <RobotList>
                    {enabledRobots.map((robot) => (
                        <DashboardRobotStatusCard key={robot.id} robot={robot} />
                    ))}
                </RobotList>
                <DashboardScheduledMissionsView />
            </DashboardColumn>
        </DashboardColumns>
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

    return (
        <DashboardZoomWrapper>
            <Header installation={installation} minimal fullWidth showBanner={false} />
            <PageBackground>
                <DashboardPageContent>
                    {isPending ? (
                        <PendingResultPlaceholder isLargeImage={true} />
                    ) : (
                        <DashboardContent inspectionData={data ?? []} />
                    )}
                </DashboardPageContent>
            </PageBackground>
        </DashboardZoomWrapper>
    )
}
