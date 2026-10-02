/**
 * A single normalised, comparable quantity extracted from an analysis result.
 *
 * SARA currently returns one loosely typed `{ value, unit, confidence }` object
 * per analysis, but the result model is expected to become a richer, per-analysis
 * document. Everything downstream of the extraction step works on measurements
 * rather than on the raw result, so only the extractor in the analysis definition
 * has to change when that happens.
 */
export interface AnalysisMeasurement {
    /**
     * Stable identifier for what is being measured, e.g. `oilFillLevel`.
     * A future multi-value result can yield several measurements with distinct
     * metric keys, and thresholds are configured per metric.
     */
    metric: string
    /** Value expressed in `unit`, already converted from the raw SARA encoding. */
    value: number
    unit?: string
    /** Model confidence as a percentage (0-100), matching SARA's result DTO. */
    confidence?: number
}

export const formatMeasurement = (measurement: AnalysisMeasurement, fractionDigits: number = 0): string => {
    const value = measurement.value.toFixed(fractionDigits)
    return measurement.unit ? `${value} ${measurement.unit}` : value
}

export const formatConfidence = (confidence?: number): string | undefined =>
    confidence === undefined || confidence === null ? undefined : `${Math.round(confidence)} %`
