import { act, useEffect } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { useScheduledMissionRuns } from 'hooks/useScheduledMissionRuns'
import { ScheduledMissionRun } from 'models/ScheduledMissionRun'

const mocks = vi.hoisted(() => ({
    get: vi.fn(),
    handlers: new Map<string, () => void>(),
}))
const api = { getScheduledMissionRuns: mocks.get }
const registerEvent = (event: string, callback: () => void) => {
    mocks.handlers.set(event, callback)
    return () => mocks.handlers.delete(event)
}
vi.mock('api/UseBackendApi', () => ({ useBackendApi: () => api }))
vi.mock('contexts/SignalRContext', () => ({
    SignalREventLabels: {
        missionDefinitionUpdated: 'updated',
        missionDefinitionCreated: 'created',
        missionDefinitionDeleted: 'deleted',
    },
    useSignalRContext: () => ({ registerEvent, connectionReady: true }),
}))

let root: Root
let container: HTMLDivElement
let result: ReturnType<typeof useScheduledMissionRuns>
const Probe = ({ installation = 'AAA' }: { installation?: string }) => {
    const current = useScheduledMissionRuns(installation)
    useEffect(() => {
        result = current
    }, [current])
    return null
}
const occurrence: ScheduledMissionRun = {
    id: 'occurrence',
    scheduledAt: '2026-10-16T23:00:00Z',
    status: 'Scheduled',
    mission: {
        id: 'mission',
        name: 'Inspection',
        installationCode: 'AAA',
        isAdHoc: false,
        tasks: [],
        inspectionArea: {
            id: 'area',
            inspectionAreaName: 'Area',
            plantCode: 'plant',
            plantName: 'Plant',
            installationCode: 'AAA',
        },
    },
}

beforeEach(() => {
    vi.useFakeTimers()
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    mocks.get.mockReset().mockResolvedValue([occurrence])
    mocks.handlers.clear()
    container = document.createElement('div')
    root = createRoot(container)
})
afterEach(() => {
    act(() => root.unmount())
    vi.useRealTimers()
    vi.unstubAllGlobals()
})

test('refreshes on schedule updates and polling, and unsubscribes on unmount', async () => {
    await act(async () => root.render(<Probe />))
    expect(result.scheduledMissionRuns).toEqual([occurrence])
    mocks.get.mockResolvedValue([{ ...occurrence, status: 'Skipped' }])
    await act(async () => mocks.handlers.get('updated')?.())
    expect(result.scheduledMissionRuns[0].status).toBe('Skipped')
    await act(async () => vi.advanceTimersByTime(30_000))
    expect(mocks.get).toHaveBeenCalledTimes(3)
    act(() => root.render(null))
    expect(mocks.handlers.size).toBe(0)
    expect(vi.getTimerCount()).toBe(0)
})

test('late results from a previous installation cannot replace the active schedule', async () => {
    let resolveOld: (value: ScheduledMissionRun[]) => void = () => {}
    mocks.get.mockReturnValueOnce(
        new Promise<ScheduledMissionRun[]>((resolve) => {
            resolveOld = resolve
        })
    )
    await act(async () => root.render(<Probe />))
    mocks.get.mockResolvedValue([])
    await act(async () => root.render(<Probe installation="BBB" />))
    await act(async () => resolveOld([occurrence]))
    expect(result.scheduledMissionRuns).toEqual([])
    expect(result.autoScheduleLoading).toBe(false)
})

test('failed retrieval is explicit and a refresh can recover', async () => {
    mocks.get.mockRejectedValueOnce(new Error('Unavailable'))
    await act(async () => root.render(<Probe />))
    expect(result.autoScheduleError).toBe(true)
    await act(async () => result.refreshScheduledRuns())
    expect(result.autoScheduleError).toBe(false)
    expect(result.scheduledMissionRuns).toEqual([occurrence])
})
