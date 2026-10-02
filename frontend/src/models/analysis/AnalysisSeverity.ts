/**
 * Severity of a single evaluated analysis result.
 *
 * Kept independent of any particular analysis type so that new analyses can be
 * added without touching the components that render severity.
 */
export enum AnalysisSeverity {
    Alarm = 'Alarm',
    Warning = 'Warning',
    Normal = 'Normal',
    Unknown = 'Unknown',
}

/** Higher number means more urgent. Used for sorting and for "worst of" reductions. */
const severityOrder: Record<AnalysisSeverity, number> = {
    [AnalysisSeverity.Alarm]: 3,
    [AnalysisSeverity.Warning]: 2,
    [AnalysisSeverity.Unknown]: 1,
    [AnalysisSeverity.Normal]: 0,
}

export const severityRank = (severity: AnalysisSeverity): number => severityOrder[severity]

export const isActionableSeverity = (severity: AnalysisSeverity): boolean =>
    severity === AnalysisSeverity.Alarm || severity === AnalysisSeverity.Warning

export const compareBySeverityDescending = (a: AnalysisSeverity, b: AnalysisSeverity): number =>
    severityRank(b) - severityRank(a)
