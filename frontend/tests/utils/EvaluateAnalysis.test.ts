import { describe, expect, it } from 'vitest'
import { AnalysisSeverity } from 'models/analysis/AnalysisSeverity'
import { SeveritySource } from 'models/analysis/AnalysisEvaluation'
import { evaluateThresholdRule, describeThresholdRule, ThresholdRule } from 'models/analysis/ThresholdRule'
import { FileType, InspectionData } from 'models/InspectionRecord'
import { AnalysisType } from 'models/MissionDefinition'
import {
    activeAlerts,
    countBySeverity,
    evaluateInspectionData,
    evaluateInspectionDataList,
    latestEvaluationPerSubject,
} from 'utils/analysis/EvaluateAnalysis'
import { defaultThresholdConfiguration, resolveThresholdRule } from 'utils/analysis/ThresholdConfiguration'
import { CLOE_FILL_LEVEL_METRIC } from 'utils/analysis/AnalysisDefinitions'

const cloeInspection = (overrides: Partial<InspectionData>): InspectionData => ({
    inspectionId: 'inspection-1',
    analysisId: 'analysis-1',
    fileType: FileType.VALUE,
    analysisType: 'cloe',
    tag: 'TAG-1',
    createdAt: new Date('2026-01-01T10:00:00Z'),
    targetPosition: { x: 0, y: 0, z: 0 },
    robotPose: { position: { x: 0, y: 0, z: 0 }, orientation: { x: 0, y: 0, z: 0, w: 1 } },
    inspectionDescription: 'Oiler',
    ...overrides,
})

describe('evaluateThresholdRule', () => {
    const below: ThresholdRule = { kind: 'below', warningAtOrBelow: 10, alarmAtOrBelow: 5 }
    const above: ThresholdRule = { kind: 'above', warningAtOrAbove: 60, alarmAtOrAbove: 80 }
    const outside: ThresholdRule = {
        kind: 'outsideRange',
        normalRange: { min: 40, max: 60 },
        alarmRange: { min: 20, max: 80 },
    }

    it('flags low values as alarm and warning, inclusive of the boundary', () => {
        expect(evaluateThresholdRule(below, 4)).toBe(AnalysisSeverity.Alarm)
        expect(evaluateThresholdRule(below, 5)).toBe(AnalysisSeverity.Alarm)
        expect(evaluateThresholdRule(below, 7)).toBe(AnalysisSeverity.Warning)
        expect(evaluateThresholdRule(below, 10)).toBe(AnalysisSeverity.Warning)
        expect(evaluateThresholdRule(below, 11)).toBe(AnalysisSeverity.Normal)
    })

    it('flags high values for above rules', () => {
        expect(evaluateThresholdRule(above, 80)).toBe(AnalysisSeverity.Alarm)
        expect(evaluateThresholdRule(above, 60)).toBe(AnalysisSeverity.Warning)
        expect(evaluateThresholdRule(above, 59)).toBe(AnalysisSeverity.Normal)
    })

    it('flags values outside the configured band', () => {
        expect(evaluateThresholdRule(outside, 50)).toBe(AnalysisSeverity.Normal)
        expect(evaluateThresholdRule(outside, 65)).toBe(AnalysisSeverity.Warning)
        expect(evaluateThresholdRule(outside, 85)).toBe(AnalysisSeverity.Alarm)
    })

    it('returns unknown for non finite values', () => {
        expect(evaluateThresholdRule(below, Number.NaN)).toBe(AnalysisSeverity.Unknown)
    })

    it('describes the crossed threshold with the unit', () => {
        expect(describeThresholdRule(below, AnalysisSeverity.Alarm, '%')).toBe('\u2264 5 %')
        expect(describeThresholdRule(below, AnalysisSeverity.Warning, '%')).toBe('\u2264 10 %')
    })
})

describe('resolveThresholdRule', () => {
    it('falls back to the analysis definition default', () => {
        const rule = resolveThresholdRule(
            defaultThresholdConfiguration,
            { analysisType: AnalysisType.CLOE, installationCode: 'NLS', tag: 'TAG-1' },
            CLOE_FILL_LEVEL_METRIC
        )
        expect(rule).toEqual({ kind: 'below', warningAtOrBelow: 10, alarmAtOrBelow: 5 })
    })

    it('prefers the most specific matching override', () => {
        const tagRule: ThresholdRule = { kind: 'below', warningAtOrBelow: 30, alarmAtOrBelow: 20 }
        const typeRule: ThresholdRule = { kind: 'below', warningAtOrBelow: 15, alarmAtOrBelow: 8 }
        const configuration = {
            overrides: [
                { analysisType: AnalysisType.CLOE, rule: typeRule },
                { analysisType: AnalysisType.CLOE, installationCode: 'NLS', tag: 'TAG-1', rule: tagRule },
            ],
        }

        expect(
            resolveThresholdRule(
                configuration,
                { analysisType: AnalysisType.CLOE, installationCode: 'NLS', tag: 'TAG-1' },
                CLOE_FILL_LEVEL_METRIC
            )
        ).toEqual(tagRule)

        expect(
            resolveThresholdRule(
                configuration,
                { analysisType: AnalysisType.CLOE, installationCode: 'NLS', tag: 'TAG-2' },
                CLOE_FILL_LEVEL_METRIC
            )
        ).toEqual(typeRule)
    })
})

describe('evaluateInspectionData', () => {
    it('converts the CLOE fraction to a percentage and applies the default rule', () => {
        const evaluation = evaluateInspectionData(
            cloeInspection({ value: '0.03', confidence: 0.9 }),
            defaultThresholdConfiguration,
            'NLS'
        )

        expect(evaluation?.measurement).toEqual({
            metric: CLOE_FILL_LEVEL_METRIC,
            value: 3,
            unit: '%',
            confidence: 0.9,
        })
        expect(evaluation?.severity).toBe(AnalysisSeverity.Alarm)
        expect(evaluation?.thresholdDescription).toBe('\u2264 5 %')
    })

    it('does not set a threshold description for normal results', () => {
        const evaluation = evaluateInspectionData(
            cloeInspection({ value: '0.8' }),
            defaultThresholdConfiguration,
            'NLS'
        )
        expect(evaluation?.severity).toBe(AnalysisSeverity.Normal)
        expect(evaluation?.thresholdDescription).toBeUndefined()
    })

    it('reports unknown severity when the result holds nothing numeric and SARA is silent', () => {
        const evaluation = evaluateInspectionData(
            cloeInspection({ value: undefined }),
            defaultThresholdConfiguration,
            'NLS'
        )
        expect(evaluation?.measurement).toBeUndefined()
        expect(evaluation?.severity).toBe(AnalysisSeverity.Unknown)
    })

    it('skips analysis types that are not in the registry', () => {
        expect(
            evaluateInspectionData(
                cloeInspection({ analysisType: 'not-a-real-analysis', value: '1' }),
                defaultThresholdConfiguration,
                'NLS'
            )
        ).toBeUndefined()
    })

    it('raises a warning from SARA for an analysis that has no threshold rule', () => {
        const evaluation = evaluateInspectionData(
            cloeInspection({ analysisType: 'fencilla', value: 'true', warning: 'Fence breach detected' }),
            defaultThresholdConfiguration,
            'NLS'
        )
        expect(evaluation?.severity).toBe(AnalysisSeverity.Warning)
        expect(evaluation?.severitySource).toBe(SeveritySource.Provider)
        expect(evaluation?.providerWarning).toBe('Fence breach detected')
    })

    it('keeps a SARA warning when the local threshold says the value is normal', () => {
        const evaluation = evaluateInspectionData(
            cloeInspection({ value: '0.8', warning: 'Lens obstructed' }),
            defaultThresholdConfiguration,
            'NLS'
        )
        expect(evaluation?.severity).toBe(AnalysisSeverity.Warning)
        expect(evaluation?.severitySource).toBe(SeveritySource.Provider)
    })

    it('lets a threshold alarm outrank a SARA warning', () => {
        const evaluation = evaluateInspectionData(
            cloeInspection({ value: '0.02', warning: 'Lens obstructed' }),
            defaultThresholdConfiguration,
            'NLS'
        )
        expect(evaluation?.severity).toBe(AnalysisSeverity.Alarm)
        expect(evaluation?.severitySource).toBe(SeveritySource.Threshold)
        expect(evaluation?.providerWarning).toBe('Lens obstructed')
    })

    it('ignores a blank SARA warning', () => {
        const evaluation = evaluateInspectionData(
            cloeInspection({ value: '0.8', warning: '   ' }),
            defaultThresholdConfiguration,
            'NLS'
        )
        expect(evaluation?.severity).toBe(AnalysisSeverity.Normal)
        expect(evaluation?.providerWarning).toBeUndefined()
    })
})

describe('aggregation', () => {
    const inspections = [
        cloeInspection({
            inspectionId: 'i1',
            analysisId: 'a1',
            tag: 'TAG-1',
            value: '0.5',
            createdAt: new Date('2026-01-01T08:00:00Z'),
        }),
        cloeInspection({
            inspectionId: 'i2',
            analysisId: 'a2',
            tag: 'TAG-1',
            value: '0.03',
            createdAt: new Date('2026-01-01T12:00:00Z'),
        }),
        cloeInspection({
            inspectionId: 'i3',
            analysisId: 'a3',
            tag: 'TAG-2',
            value: '0.08',
            createdAt: new Date('2026-01-01T09:00:00Z'),
        }),
    ]

    const evaluations = latestEvaluationPerSubject(
        evaluateInspectionDataList(inspections, defaultThresholdConfiguration, 'NLS')
    )

    it('keeps only the newest evaluation per tag', () => {
        expect(evaluations).toHaveLength(2)
        expect(evaluations.find((e) => e.subject.tag === 'TAG-1')?.analysisId).toBe('a2')
    })

    it('sorts alerts with alarms before warnings', () => {
        const alerts = activeAlerts(evaluations)
        expect(alerts.map((a) => a.severity)).toEqual([AnalysisSeverity.Alarm, AnalysisSeverity.Warning])
    })

    it('counts evaluations per severity', () => {
        expect(countBySeverity(evaluations)).toMatchObject({
            [AnalysisSeverity.Alarm]: 1,
            [AnalysisSeverity.Warning]: 1,
            [AnalysisSeverity.Normal]: 0,
        })
    })
})
