import { AnalysisSubject } from 'models/analysis/AnalysisEvaluation'
import { ThresholdRule } from 'models/analysis/ThresholdRule'
import { AnalysisType } from 'models/MissionDefinition'
import { getAnalysisDefinition } from './AnalysisDefinitions'

/**
 * A threshold rule scoped to some part of the analysis/installation/equipment
 * hierarchy. Leaving a field out widens the scope.
 */
export interface ThresholdOverride {
    analysisType: AnalysisType
    /** Metric the rule applies to. Defaults to the analysis definition's metric. */
    metric?: string
    installationCode?: string
    /** Equipment tag. The most specific scope available today. */
    tag?: string
    rule: ThresholdRule
}

export interface ThresholdConfiguration {
    overrides: ThresholdOverride[]
}

/**
 * Overrides are empty for now: every analysis falls back to the default rule on
 * its definition. The lookup is already scope aware so that per-installation and
 * per-tag thresholds can be introduced, or loaded from an API, without touching
 * any caller.
 */
export const defaultThresholdConfiguration: ThresholdConfiguration = { overrides: [] }

const matchesScope = (override: ThresholdOverride, subject: AnalysisSubject, metric: string): boolean => {
    if (override.analysisType !== subject.analysisType) return false
    if (override.metric !== undefined && override.metric !== metric) return false
    if (
        override.installationCode !== undefined &&
        !equalsIgnoreCase(override.installationCode, subject.installationCode)
    )
        return false
    if (override.tag !== undefined && !equalsIgnoreCase(override.tag, subject.tag)) return false
    return true
}

const equalsIgnoreCase = (a?: string, b?: string): boolean =>
    a !== undefined && b !== undefined && a.toLowerCase() === b.toLowerCase()

/** More specific scopes win. Tag is more specific than installation. */
const specificity = (override: ThresholdOverride): number =>
    (override.tag !== undefined ? 4 : 0) +
    (override.installationCode !== undefined ? 2 : 0) +
    (override.metric !== undefined ? 1 : 0)

export const resolveThresholdRule = (
    configuration: ThresholdConfiguration,
    subject: AnalysisSubject,
    metric: string
): ThresholdRule | undefined => {
    const match = configuration.overrides
        .filter((override) => matchesScope(override, subject, metric))
        .sort((a, b) => specificity(b) - specificity(a))[0]

    return match?.rule ?? getAnalysisDefinition(subject.analysisType)?.defaultRule
}
