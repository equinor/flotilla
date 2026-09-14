import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { FileType, InspectionData } from 'models/InspectionRecord'
import { SmallAnalysisResult } from 'pages/InspectionReportPage/InspectionReportImage'
import { queryClient } from '../../src/App'
import { InspectionsProvider, useInspectionsContext } from 'contexts/InspectionsContext'
import { SignalREventLabels } from 'contexts/SignalRContext'

vi.mock('../../src/App', async () => {
    const { QueryClient } = await import('@tanstack/react-query')
    return { queryClient: new QueryClient() }
})

const { handlers, registerEvent, saraApi } = vi.hoisted(() => {
    const handlers = new Map<string, (username: string, message: string) => void>()
    return {
        handlers,
        registerEvent: (event: string, handler: (username: string, message: string) => void) => {
            handlers.set(event, handler)
            return () => {
                handlers.delete(event)
            }
        },
        saraApi: {
            getSaraData: vi.fn(),
            getSaraDataByInspectionId: vi.fn(),
        },
    }
})

vi.mock('contexts/SignalRContext', () => ({
    SignalREventLabels: {
        inspectionVisualizationReady: 'Inspection Visulization Ready',
        analysisResultReady: 'Analysis Result Ready',
    },
    useSignalRContext: () => ({ connectionReady: true, registerEvent }),
}))
vi.mock('api/UseBackendApi', () => ({ useBackendApi: () => undefined }))
vi.mock('api/UseSaraApi', () => ({ useSaraApi: () => saraApi }))
vi.mock('contexts/LanguageContext', () => ({
    useLanguageContext: () => ({ TranslateText: (text: string) => text }),
}))
vi.mock('pages/InspectionReportPage/InspectionVideoPlayer', () => ({
    VideoPlaceholder: () => null,
    VideoPlayer: () => null,
}))

const inspection: InspectionData = {
    inspectionId: 'inspection-1',
    analysisId: 'analysis-1',
    fileType: FileType.IMAGE,
    anonymizedSAS: 'https://example.test/inspection.jpg',
    tag: 'tag-1',
    createdAt: new Date('2026-09-14T09:00:00Z'),
    targetPosition: { x: 0, y: 0, z: 0 },
    robotPose: {
        position: { x: 0, y: 0, z: 0 },
        orientation: { x: 0, y: 0, z: 0, w: 1 },
    },
    inspectionDescription: 'Oil level',
}
const analyzedInspection: InspectionData = {
    ...inspection,
    visualizedSAS: 'https://example.test/analysis.jpg',
    value: '0.8',
}

let container: HTMLDivElement
let root: Root

beforeEach(() => {
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    vi.useFakeTimers()
    handlers.clear()
    saraApi.getSaraData.mockReset().mockResolvedValue([inspection])
    saraApi.getSaraDataByInspectionId.mockReset().mockResolvedValue(inspection)
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    queryClient.clear()
    container.remove()
    vi.useRealTimers()
    vi.unstubAllGlobals()
})

const flushUpdates = async () => {
    await act(async () => {
        await vi.advanceTimersByTimeAsync(1)
    })
}

const MissionResults = () => {
    const { useSaraListData } = useInspectionsContext()
    const { data } = useSaraListData([inspection.inspectionId], null, null, null, null, null)
    return (
        <>
            <span data-testid="task-value">{data?.[0]?.value ?? 'pending'}</span>
            <SmallAnalysisResult inspectionId={inspection.inspectionId} />
        </>
    )
}

test.each([SignalREventLabels.analysisResultReady, SignalREventLabels.inspectionVisualizationReady])(
    '%s refreshes both task data and a recently cached analysis thumbnail',
    async (event) => {
        const unrelatedInspection = { ...inspection, inspectionId: 'unrelated-inspection' }
        queryClient.setQueryData(['fetchInspectionData', unrelatedInspection.inspectionId], unrelatedInspection)
        await act(async () => {
            root.render(
                <QueryClientProvider client={queryClient}>
                    <InspectionsProvider>
                        <MissionResults />
                    </InspectionsProvider>
                </QueryClientProvider>
            )
        })
        await flushUpdates()
        expect(container.textContent).toContain('No analysis available')
        expect(saraApi.getSaraDataByInspectionId).toHaveBeenCalledTimes(1)

        saraApi.getSaraData.mockResolvedValue([analyzedInspection])
        saraApi.getSaraDataByInspectionId.mockResolvedValue(analyzedInspection)
        await act(async () => {
            handlers.get(event)!('all', JSON.stringify({ inspectionId: inspection.inspectionId }))
        })
        await flushUpdates()

        expect(container.querySelector('[data-testid="task-value"]')?.textContent).toBe('0.8')
        expect(saraApi.getSaraDataByInspectionId).toHaveBeenCalledTimes(2)
        expect(saraApi.getSaraDataByInspectionId).toHaveBeenLastCalledWith(inspection.inspectionId)
        expect(container.querySelector('img')?.getAttribute('src')).toBe(analyzedInspection.visualizedSAS)
        expect(queryClient.getQueryData(['fetchInspectionData', unrelatedInspection.inspectionId])).toEqual(
            unrelatedInspection
        )

        // Remounting the observer should still reuse its ten-minute cache.
        await act(async () => {
            root.render(
                <QueryClientProvider client={queryClient}>
                    <InspectionsProvider>
                        <MissionResults key="remounted" />
                    </InspectionsProvider>
                </QueryClientProvider>
            )
        })
        await flushUpdates()
        expect(saraApi.getSaraDataByInspectionId).toHaveBeenCalledTimes(2)
    }
)
