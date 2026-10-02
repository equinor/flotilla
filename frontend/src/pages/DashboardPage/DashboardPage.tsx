import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useContext, useMemo } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { ContentCard, PageBackground } from 'components/Styles/StyledComponents'
import { InstallationContext } from 'contexts/InstallationContext'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { AnalysisType } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import { AnalysisEvaluation } from 'models/analysis/AnalysisEvaluation'
import { AnalysisSeverity } from 'models/analysis/AnalysisSeverity'
import { useAnalysisEvaluations } from 'hooks/useAnalysisEvaluations'
import { DashboardAlertPanel } from './DashboardAlertPanel'
import { DashboardRobotStatusCard } from './DashboardRobotStatusCard'
import { DashboardScheduledMissionsView } from './DashboardScheduledMissionsView'
import { useAssetContext } from 'contexts/AssetContext'
import { DashboardInspectionsPlantMap } from './DashboardInspectionsPlantMap'
import styled from 'styled-components'

interface DataViewContentProps {
    alerts: AnalysisEvaluation[]
    severityCounts: Record<AnalysisSeverity, number>
    alertInspections: InspectionData[]
    hasLoadingError: boolean
}

const DashboardPageContent = styled.div`
    width: 100%;
    min-width: 0;
    padding-inline: var(--eds-page-space-horizontal);
    display: flex;
    flex-direction: column;
    gap: 2rem;
    --dashboard-zoom: clamp(1, calc(100vw / 2195px), 3);
    zoom: var(--dashboard-zoom);
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

const PageTitle = styled(Typography)`
    padding-left: 8px;
`

const DashboardContent = ({ alerts, severityCounts, alertInspections, hasLoadingError }: DataViewContentProps) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const { installationInspectionAreas } = useAssetContext()
    const { enabledRobots } = useAssetContext()

    const plantCode =
        installationInspectionAreas.find((i) => i.installationCode === installation.installationCode)?.plantCode ?? null

    return (
        <>
            <PageTitle variant="h2">{`${installation.name} ${TranslateText('Dashboard')}`}</PageTitle>
            <DashboardColumns>
                <DashboardColumn>
                    <DashboardAlertPanel
                        alerts={alerts}
                        severityCounts={severityCounts}
                        hasLoadingError={hasLoadingError}
                    />
                </DashboardColumn>
                <DashboardColumn>
                    {plantCode && (
                        <MapCard>
                            <DashboardInspectionsPlantMap
                                key={'all'}
                                plantCode={plantCode}
                                floorId="0"
                                inspections={alertInspections}
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
        </>
    )
}

/** Analyses surfaced on the dashboard. Adding a type here is the whole onboarding step. */
const dashboardAnalysisTypes = [AnalysisType.CLOE]

export const DashboardPage = () => {
    const { alerts, severityCounts, rawData, isPending, isError } = useAnalysisEvaluations({
        analysisTypes: dashboardAnalysisTypes,
    })

    // The map plots raw records, so map the alerts back to the rows they came from.
    const alertInspections = useMemo(() => {
        const alertIds = new Set(alerts.map((alert) => alert.analysisId))
        return rawData.filter((inspection) => alertIds.has(inspection.analysisId))
    }, [alerts, rawData])

    return (
        <PageBackground>
            <DashboardPageContent>
                {isPending ? (
                    <PendingResultPlaceholder isLargeImage={true} />
                ) : (
                    <DashboardContent
                        alerts={alerts}
                        severityCounts={severityCounts}
                        alertInspections={alertInspections}
                        hasLoadingError={isError}
                    />
                )}
            </DashboardPageContent>
        </PageBackground>
    )
}
