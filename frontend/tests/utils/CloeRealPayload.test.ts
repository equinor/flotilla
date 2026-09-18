import { describe, expect, it } from 'vitest'
import { AnalysisSeverity } from '../../src/models/analysis/AnalysisSeverity'
import { FileType, InspectionData } from '../../src/models/InspectionRecord'
import { activeAlerts, evaluateInspectionDataList } from '../../src/utils/analysis/EvaluateAnalysis'
import { defaultThresholdConfiguration } from '../../src/utils/analysis/ThresholdConfiguration'

// Values copied verbatim from a local SARA run: the API renders CLOE oilLevel
// with "F5", so the frontend receives a fixed-point string, not a number.
const record = (tag: string, value: string, warning?: string): InspectionData =>
    ({
        inspectionId: `insp-${tag}`,
        analysisId: `an-${tag}`,
        analysisType: 'cloe',
        tag,
        createdAt: new Date('2026-10-02T10:50:00Z'),
        fileType: FileType.IMAGE,
        inspectionDescription: '',
        value,
        unit: '',
        confidence: 100,
        warning,
    }) as InspectionData

describe('real SARA CLOE payloads', () => {
    const evaluate = (data: InspectionData[]) => evaluateInspectionDataList(data, defaultThresholdConfiguration, 'NLS')

    it('raises an alarm for an empty oiler', () => {
        const [evaluation] = evaluate([record('cloe-empty', '0.00000', 'Oil level is below 5 %')])
        expect(evaluation.severity).toBe(AnalysisSeverity.Alarm)
        expect(evaluation.measurement?.value).toBe(0)
    })

    it('leaves a healthy oiler out of the alert list', () => {
        const evaluations = evaluate([record('cloe-normal', '0.68000')])
        expect(evaluations[0].severity).toBe(AnalysisSeverity.Normal)
        expect(activeAlerts(evaluations)).toHaveLength(0)
    })

    it('surfaces exactly the two failing tags from the local run', () => {
        const alerts = activeAlerts(
            evaluate([
                record('cloe-normal', '0.68000'),
                record('cloe-normal2', '0.67000'),
                record('cloe-low', '0.00000', 'Oil level is below 5 %'),
                record('cloe-empty', '0.00000', 'Oil level is below 5 %'),
            ])
        )
        expect(alerts.map((a) => a.subject.tag)).toEqual(['cloe-low', 'cloe-empty'])
    })
})
