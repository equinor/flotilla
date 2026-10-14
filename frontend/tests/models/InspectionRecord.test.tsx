import { act } from 'react'
import { createRoot } from 'react-dom/client'
import { expect, test, vi } from 'vitest'
import { InspectionRecord, inspectionRecordToInspectionData } from 'models/InspectionRecord'
import { ResultAnalysis } from 'pages/MissionPage/MissionResults/ResultAnalysis'

vi.mock('contexts/LanguageContext', () => ({
    useLanguageContext: () => ({ TranslateText: (text: string) => text }),
}))

const record: InspectionRecord = {
    id: 'record-1',
    inspectionId: 'inspection-1',
    installationCode: 'installation',
    createdAt: new Date('2026-10-14T06:00:00Z'),
    inspectionType: 'image',
    tag: 'tag-1',
    targetPosition: { x: 0, y: 0, z: 0 },
    robotPose: {
        position: { x: 0, y: 0, z: 0 },
        orientation: { x: 0, y: 0, z: 0, w: 1 },
    },
    inspectionDescription: 'Inspection',
    robotName: 'robot-1',
    timestamp: 0,
    analysisGroupId: 'group-1',
    analyses: [
        {
            id: 'analysis-1',
            name: 'Oil level',
            analysisType: 'cloe',
            createdAt: '2026-10-14T06:00:00Z',
            anonymizedSAS: 'https://example.test/inspection.jpg',
            result: {
                analysisId: 'analysis-1',
                analysisType: 'cloe',
                value: null,
                unit: null,
                confidence: null,
                warning: null,
            },
        },
    ],
}

test('a nullable API result does not crash analysis rendering', async () => {
    const inspection = inspectionRecordToInspectionData(record)!
    expect(inspection.value).toBeUndefined()
    expect(inspection.unit).toBeUndefined()
    expect(inspection.confidence).toBeUndefined()
    expect(inspection.warning).toBeUndefined()

    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    const container = document.createElement('div')
    const root = createRoot(container)
    try {
        await act(async () => root.render(<ResultAnalysis inspection={inspection} />))
        expect(container.textContent).toBe('')
        await act(async () => root.render(<ResultAnalysis inspection={{ ...inspection, value: '0.8' }} />))
        expect(container.textContent).toBe('Level: 80 %')
    } finally {
        act(() => root.unmount())
        vi.unstubAllGlobals()
    }
})
