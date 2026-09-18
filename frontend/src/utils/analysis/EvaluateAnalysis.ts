import { AnalysisEvaluation, AnalysisSubject, SeveritySource } from 'models/analysis/AnalysisEvaluation'
import {
    AnalysisSeverity,
    compareBySeverityDescending,
    isActionableSeverity,
    severityRank,
} from 'models/analysis/AnalysisSeverity'
import { describeThresholdRule, evaluateThresholdRule } from 'models/analysis/ThresholdRule'
import { InspectionData } from 'models/InspectionRecord'
import { saraAnalysisTypeToEnum } from 'models/SaraAnalysisTypeMapping'
import { getAnalysisDefinition } from './AnalysisDefinitions'
import { resolveThresholdRule, ThresholdConfiguration } from './ThresholdConfiguration'

/**
 * Judge one SARA inspection result, or return undefined when the analysis type
 * is not onboarded.
 *
 * Two sources can raise an alert: a locally configured threshold, and a warning
 * SARA attached to the result. The more severe of the two wins, so adding local
 * thresholds can only ever sharpen an alert, never silently hide one that SARA
 * already flagged.
 */
export const evaluateInspectionData = (
    data: InspectionData,
    configuration: ThresholdConfiguration,
    installationCode?: string
): AnalysisEvaluation | undefined => {
    const analysisType = saraAnalysisTypeToEnum(data.analysisType)
    const definition = getAnalysisDefinition(analysisType)
    if (!definition || analysisType === undefined) return undefined

    const subject: AnalysisSubject = { analysisType, installationCode, tag: data.tag }
    const measurement = definition.toMeasurement?.(data)

    const rule =
        measurement !== undefined ? resolveThresholdRule(configuration, subject, measurement.metric) : undefined
    const thresholdSeverity =
        rule && measurement ? evaluateThresholdRule(rule, measurement.value) : AnalysisSeverity.Unknown

    const providerWarning = data.warning?.trim() ? data.warning.trim() : undefined
    const providerSeverity = providerWarning ? AnalysisSeverity.Warning : AnalysisSeverity.Normal

    const usesThreshold = severityRank(thresholdSeverity) >= severityRank(providerSeverity)
    const severity = usesThreshold ? thresholdSeverity : providerSeverity

    return {
        inspectionId: data.inspectionId,
        analysisId: data.analysisId,
        subject,
        analysisLabel: definition.label,
        valueLabel: definition.valueLabel,
        image: definition.image,
        inspectionDescription: data.inspectionDescription,
        observedAt: new Date(data.createdAt),
        measurement,
        rawValue: data.value,
        rawUnit: data.unit,
        rawAnalysisType: data.analysisType,
        severity,
        severitySource: usesThreshold ? SeveritySource.Threshold : SeveritySource.Provider,
        providerWarning,
        rule,
        thresholdDescription:
            rule && isActionableSeverity(thresholdSeverity)
                ? describeThresholdRule(rule, thresholdSeverity, measurement?.unit)
                : undefined,
    }
}

export const evaluateInspectionDataList = (
    dataList: InspectionData[],
    configuration: ThresholdConfiguration,
    installationCode?: string
): AnalysisEvaluation[] =>
    dataList
        .map((data) => evaluateInspectionData(data, configuration, installationCode))
        .filter((evaluation): evaluation is AnalysisEvaluation => evaluation !== undefined)

/**
 * The dashboard shows current state, not history, so collapse to the newest
 * evaluation per analysis type and tag.
 */
export const latestEvaluationPerSubject = (evaluations: AnalysisEvaluation[]): AnalysisEvaluation[] => {
    const latest = new Map<string, AnalysisEvaluation>()

    evaluations.forEach((evaluation) => {
        const key = `${evaluation.subject.analysisType}|${evaluation.subject.tag ?? evaluation.inspectionId}`
        const current = latest.get(key)
        if (!current || evaluation.observedAt.getTime() > current.observedAt.getTime()) latest.set(key, evaluation)
    })

    return [...latest.values()]
}

/** Most urgent first, then most recent first. */
export const sortByUrgency = (evaluations: AnalysisEvaluation[]): AnalysisEvaluation[] =>
    [...evaluations].sort(
        (a, b) => compareBySeverityDescending(a.severity, b.severity) || b.observedAt.getTime() - a.observedAt.getTime()
    )

export const activeAlerts = (evaluations: AnalysisEvaluation[]): AnalysisEvaluation[] =>
    sortByUrgency(evaluations.filter((evaluation) => isActionableSeverity(evaluation.severity)))

export const countBySeverity = (evaluations: AnalysisEvaluation[]): Record<AnalysisSeverity, number> => {
    const counts: Record<AnalysisSeverity, number> = {
        [AnalysisSeverity.Alarm]: 0,
        [AnalysisSeverity.Warning]: 0,
        [AnalysisSeverity.Normal]: 0,
        [AnalysisSeverity.Unknown]: 0,
    }
    evaluations.forEach((evaluation) => (counts[evaluation.severity] += 1))
    return counts
}
