import { useContext, useMemo } from 'react'
import { useInspectionsContext } from 'contexts/InspectionsContext'
import { InstallationContext } from 'contexts/InstallationContext'
import { AnalysisEvaluation } from 'models/analysis/AnalysisEvaluation'
import { AnalysisSeverity } from 'models/analysis/AnalysisSeverity'
import { AnalysisType } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import { saraAnalysisTypeToEnum } from 'models/SaraAnalysisTypeMapping'
import { createPresetTimeRange, PresetDays } from 'pages/DataViewPage/DataViewTimeRange'
import {
    activeAlerts,
    countBySeverity,
    evaluateInspectionDataList,
    latestEvaluationPerSubject,
    sortByUrgency,
} from 'utils/analysis/EvaluateAnalysis'
import { defaultThresholdConfiguration, ThresholdConfiguration } from 'utils/analysis/ThresholdConfiguration'

interface UseAnalysisEvaluationsOptions {
    /** Analyses to include. Only types present in the analysis registry are evaluated. */
    analysisTypes: AnalysisType[]
    /** How far back to look for results. */
    lookbackDays?: PresetDays
    thresholdConfiguration?: ThresholdConfiguration
}

export interface AnalysisEvaluationsResult {
    /** Newest evaluation per analysis type and tag, most urgent first. */
    evaluations: AnalysisEvaluation[]
    /** The subset that is in warning or alarm. */
    alerts: AnalysisEvaluation[]
    severityCounts: Record<AnalysisSeverity, number>
    /** Unprocessed records, for consumers such as the map that plot every inspection. */
    rawData: InspectionData[]
    isPending: boolean
    isError: boolean
}

const defaultLookbackDays: PresetDays = 30

/**
 * Loads SARA results for the current installation and evaluates them against the
 * configured thresholds.
 *
 * SARA can only filter on one analysis type per request, so a single type is
 * pushed to the query and anything wider is filtered client side. That keeps the
 * hook call count fixed regardless of how many analyses the dashboard shows.
 */
export const useAnalysisEvaluations = ({
    analysisTypes,
    lookbackDays = defaultLookbackDays,
    thresholdConfiguration = defaultThresholdConfiguration,
}: UseAnalysisEvaluationsOptions): AnalysisEvaluationsResult => {
    const { installation } = useContext(InstallationContext)
    const { useSaraListData } = useInspectionsContext()

    const installationCode = installation.installationCode

    // The preset snaps to midnight, so the query key stays stable between renders
    // instead of producing a new cache entry every time.
    const timeRange = useMemo(() => createPresetTimeRange(lookbackDays), [lookbackDays])

    const serverSideAnalysisType = analysisTypes.length === 1 ? analysisTypes[0] : null

    // analysisTypes is usually an array literal at the call site, so key the memo
    // on its contents rather than its identity to avoid recomputing every render.
    const analysisTypesKey = analysisTypes.join(',')

    const { data, isPending, isError } = useSaraListData(
        null,
        installationCode,
        null,
        serverSideAnalysisType,
        timeRange.minDate,
        timeRange.maxDate
    )

    const rawData = useMemo(() => {
        if (!data) return []
        const requested = new Set(analysisTypesKey.split(',') as AnalysisType[])
        return data.filter((inspection) => {
            const analysisType = saraAnalysisTypeToEnum(inspection.analysisType)
            return analysisType !== undefined && requested.has(analysisType)
        })
    }, [data, analysisTypesKey])

    const evaluations = useMemo(
        () =>
            sortByUrgency(
                latestEvaluationPerSubject(
                    evaluateInspectionDataList(rawData, thresholdConfiguration, installationCode)
                )
            ),
        [rawData, thresholdConfiguration, installationCode]
    )

    return {
        evaluations,
        alerts: useMemo(() => activeAlerts(evaluations), [evaluations]),
        severityCounts: useMemo(() => countBySeverity(evaluations), [evaluations]),
        rawData,
        isPending,
        isError,
    }
}
