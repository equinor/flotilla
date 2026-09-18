import { AnalysisMeasurement } from './AnalysisMeasurement'
import { AnalysisSeverity } from './AnalysisSeverity'
import { ThresholdRule } from './ThresholdRule'
import { AnalysisType } from 'models/MissionDefinition'

/** Where a measurement came from. Also the lookup key for threshold resolution. */
export interface AnalysisSubject {
    analysisType: AnalysisType
    installationCode?: string
    /** Equipment tag, e.g. the constant level oiler being inspected. */
    tag?: string
}

/**
 * What decided the severity.
 *
 * `Threshold` means a configured rule was applied locally. `Provider` means SARA
 * flagged the result itself and no local rule judged it more severely. Keeping
 * these apart lets the UI explain an alert, and lets analyses without a rule
 * still raise one.
 */
export enum SeveritySource {
    Threshold = 'Threshold',
    Provider = 'Provider',
}

/**
 * The result of judging one analysis result.
 *
 * `rule` and `thresholdDescription` are carried along so the UI can explain a
 * severity without re-resolving configuration.
 */
export interface AnalysisEvaluation {
    inspectionId: string
    analysisId: string
    subject: AnalysisSubject
    /** Label of the analysis definition, e.g. "Constant level oiler". */
    analysisLabel: string
    /** Label of the measured quantity, e.g. "Fill level". */
    valueLabel: string
    image?: string
    inspectionDescription?: string
    observedAt: Date
    /** Absent for analyses that report nothing numeric, such as a boolean detection. */
    measurement?: AnalysisMeasurement
    /** Value exactly as SARA reported it, for rendering analyses without an extractor. */
    rawValue?: string
    rawUnit?: string
    /** SARA's own analysis type string, which the shared value display keys on. */
    rawAnalysisType?: string
    severity: AnalysisSeverity
    severitySource: SeveritySource
    /** Free text SARA attached to the result, shown alongside the severity. */
    providerWarning?: string
    rule?: ThresholdRule
    /** Short text for the threshold that was crossed, absent when none was. */
    thresholdDescription?: string
}
