import { AnalysisSeverity } from './AnalysisSeverity'

/**
 * Declarative description of when a measurement counts as a warning or an alarm.
 *
 * Rules are data, never code, so that they can eventually be served from the
 * backend or from SARA per analysis/equipment without any frontend change. New
 * rule shapes are added as new members of this union, and `evaluateThresholdRule`
 * is the single place that has to learn about them.
 */
export type ThresholdRule = BelowThresholdRule | AboveThresholdRule | OutsideRangeThresholdRule

/** Low values are bad, e.g. an oil level that is running empty. */
export interface BelowThresholdRule {
    kind: 'below'
    warningAtOrBelow: number
    alarmAtOrBelow: number
}

/** High values are bad, e.g. an overheating component. */
export interface AboveThresholdRule {
    kind: 'above'
    warningAtOrAbove: number
    alarmAtOrAbove: number
}

/** Values are expected to stay inside a band, e.g. a pressure set point. */
export interface OutsideRangeThresholdRule {
    kind: 'outsideRange'
    /** Values outside this range are at least a warning. */
    normalRange: { min: number; max: number }
    /** Values outside this wider range are an alarm. */
    alarmRange: { min: number; max: number }
}

export const evaluateThresholdRule = (rule: ThresholdRule, value: number): AnalysisSeverity => {
    if (!Number.isFinite(value)) return AnalysisSeverity.Unknown

    switch (rule.kind) {
        case 'below':
            if (value <= rule.alarmAtOrBelow) return AnalysisSeverity.Alarm
            if (value <= rule.warningAtOrBelow) return AnalysisSeverity.Warning
            return AnalysisSeverity.Normal
        case 'above':
            if (value >= rule.alarmAtOrAbove) return AnalysisSeverity.Alarm
            if (value >= rule.warningAtOrAbove) return AnalysisSeverity.Warning
            return AnalysisSeverity.Normal
        case 'outsideRange':
            if (value < rule.alarmRange.min || value > rule.alarmRange.max) return AnalysisSeverity.Alarm
            if (value < rule.normalRange.min || value > rule.normalRange.max) return AnalysisSeverity.Warning
            return AnalysisSeverity.Normal
    }
}

const withUnit = (value: number, unit?: string): string => (unit ? `${value} ${unit}` : `${value}`)

/** Human readable summary of a rule, used to explain why a row is flagged. */
export const describeThresholdRule = (rule: ThresholdRule, severity: AnalysisSeverity, unit?: string): string => {
    switch (rule.kind) {
        case 'below':
            return severity === AnalysisSeverity.Alarm
                ? `\u2264 ${withUnit(rule.alarmAtOrBelow, unit)}`
                : `\u2264 ${withUnit(rule.warningAtOrBelow, unit)}`
        case 'above':
            return severity === AnalysisSeverity.Alarm
                ? `\u2265 ${withUnit(rule.alarmAtOrAbove, unit)}`
                : `\u2265 ${withUnit(rule.warningAtOrAbove, unit)}`
        case 'outsideRange': {
            const range = severity === AnalysisSeverity.Alarm ? rule.alarmRange : rule.normalRange
            return `${withUnit(range.min, unit)} - ${withUnit(range.max, unit)}`
        }
    }
}
