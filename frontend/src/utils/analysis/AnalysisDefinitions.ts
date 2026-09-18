import { AnalysisMeasurement } from 'models/analysis/AnalysisMeasurement'
import { ThresholdRule } from 'models/analysis/ThresholdRule'
import { AnalysisType } from 'models/MissionDefinition'
import { InspectionData } from 'models/InspectionRecord'
import cloe from 'mediaAssets/cloe.png'
import fenceBreach from 'mediaAssets/fenceBreach.png'
import thermalReading from 'mediaAssets/thermalReading.png'

/**
 * Everything the dashboard needs to know about one kind of analysis: how to read
 * its result, how to judge it, and how to present it.
 *
 * This is the seam between SARA's result payload and the generic evaluation and
 * rendering code. When SARA moves to a richer, per-analysis result document only
 * `toMeasurement` has to change; nothing downstream reads the raw result.
 */
export interface AnalysisDefinition {
    analysisType: AnalysisType
    /** Untranslated label, passed through `TranslateText` at render time. */
    label: string
    /** Untranslated label for the measured quantity, e.g. "Fill level". */
    valueLabel: string
    image?: string
    /** Metric key produced by `toMeasurement`, used when resolving thresholds. */
    metric?: string
    unit?: string
    /**
     * Normalise the SARA result into a comparable measurement, or return
     * undefined when the analysis produced nothing numeric. Analyses without an
     * extractor can still raise alerts from the warning text SARA supplies.
     */
    toMeasurement?: (data: InspectionData) => AnalysisMeasurement | undefined
    /** Applied when no more specific override matches the subject. */
    defaultRule?: ThresholdRule
}

const parseNumber = (value?: string): number | undefined => {
    if (value === undefined || value === null || value.trim() === '') return undefined
    const parsed = Number.parseFloat(value)
    return Number.isFinite(parsed) ? parsed : undefined
}

export const CLOE_FILL_LEVEL_METRIC = 'oilFillLevel'

/**
 * CLOE reports the oil fill level as a 0-1 fraction; the whole application
 * presents it as a percentage, so the conversion happens here once.
 */
const cloeDefinition: AnalysisDefinition = {
    analysisType: AnalysisType.CLOE,
    label: 'Constant level oiler',
    valueLabel: 'Fill level',
    image: cloe,
    metric: CLOE_FILL_LEVEL_METRIC,
    unit: '%',
    toMeasurement: (data) => {
        const fraction = parseNumber(data.value)
        if (fraction === undefined) return undefined
        return {
            metric: CLOE_FILL_LEVEL_METRIC,
            value: fraction * 100,
            unit: '%',
            confidence: data.confidence,
        }
    },
    defaultRule: { kind: 'below', warningAtOrBelow: 10, alarmAtOrBelow: 5 },
}

/**
 * Registry of the analyses the dashboard understands.
 *
 * Only CLOE has a threshold rule so far. The others carry presentation metadata
 * only, so they still render correctly from a SARA-supplied warning, and gain
 * precise alarm/warning levels as soon as an extractor and a rule are added here.
 */
const analysisDefinitions: Partial<Record<AnalysisType, AnalysisDefinition>> = {
    [AnalysisType.CLOE]: cloeDefinition,
    [AnalysisType.Fencilla]: {
        analysisType: AnalysisType.Fencilla,
        label: 'Perimeter breach detection',
        valueLabel: 'Value',
        image: fenceBreach,
    },
    [AnalysisType.ThermalReading]: {
        analysisType: AnalysisType.ThermalReading,
        label: 'Thermal reading',
        valueLabel: 'Temperature',
        image: thermalReading,
    },
    [AnalysisType.CO2]: {
        analysisType: AnalysisType.CO2,
        label: 'CO2Measurement',
        valueLabel: 'CO2Measurement',
    },
}

export const getAnalysisDefinition = (analysisType?: AnalysisType): AnalysisDefinition | undefined =>
    analysisType !== undefined ? analysisDefinitions[analysisType] : undefined
