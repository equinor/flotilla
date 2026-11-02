import { act, useEffect } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { useAutoScheduleOccurrences } from 'hooks/useAutoScheduleOccurrences'
import { AutoScheduleOccurrence } from 'models/AutoScheduleOccurrence'

const mocks = vi.hoisted(() => ({
    get: vi.fn(),
    connectionReady: true,
}))
const api = { getAutoScheduleOccurrences: mocks.get }
vi.mock('api/UseBackendApi', () => ({ useBackendApi: () => api }))
vi.mock('contexts/SignalRContext', () => ({
    useSignalRContext: () => ({ connectionReady: mocks.connectionReady }),
}))

let root: Root
let container: HTMLDivElement
let result: ReturnType<typeof useAutoScheduleOccurrences>
const Probe = ({
    installation = 'AAA',
    definitions = initialDefinitions,
}: {
    installation?: string
    definitions?: typeof initialDefinitions
}) => {
    const current = useAutoScheduleOccurrences(installation, definitions)
    useEffect(() => {
        result = current
    }, [current])
    return null
}
const occurrence: AutoScheduleOccurrence = {
    scheduleKey: 'mission-time-key',
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
const initialDefinitions = [occurrence.mission]

beforeEach(() => {
    vi.useFakeTimers()
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    mocks.get.mockReset().mockResolvedValue([occurrence])
    mocks.connectionReady = true
    container = document.createElement('div')
    root = createRoot(container)
})
afterEach(() => {
    act(() => root.unmount())
    vi.useRealTimers()
    vi.unstubAllGlobals()
})

test('refreshes on definition changes, reconnect and polling, and clears its timer on unmount', async () => {
    await act(async () => root.render(<Probe />))
    expect(result.autoScheduleOccurrences).toEqual([occurrence])
    mocks.get.mockResolvedValue([{ ...occurrence, status: 'Skipped' }])
    await act(async () => root.render(<Probe definitions={[...initialDefinitions]} />))
    expect(result.autoScheduleOccurrences[0].status).toBe('Skipped')
    mocks.connectionReady = false
    await act(async () => root.render(<Probe />))
    const beforeReconnect = mocks.get.mock.calls.length
    mocks.connectionReady = true
    await act(async () => root.render(<Probe />))
    expect(mocks.get).toHaveBeenCalledTimes(beforeReconnect + 1)
    const beforePolling = mocks.get.mock.calls.length
    await act(async () => vi.advanceTimersByTime(30_000))
    expect(mocks.get).toHaveBeenCalledTimes(beforePolling + 1)
    act(() => root.render(null))
    expect(vi.getTimerCount()).toBe(0)
})

test('late results from a previous installation cannot replace the active schedule', async () => {
    let resolveOld: (value: AutoScheduleOccurrence[]) => void = () => {}
    mocks.get.mockReturnValueOnce(
        new Promise<AutoScheduleOccurrence[]>((resolve) => {
            resolveOld = resolve
        })
    )
    await act(async () => root.render(<Probe />))
    mocks.get.mockResolvedValue([])
    await act(async () => root.render(<Probe installation="BBB" />))
    await act(async () => resolveOld([occurrence]))
    expect(result.autoScheduleOccurrences).toEqual([])
    expect(result.autoScheduleLoading).toBe(false)
})

test('failed retrieval is explicit and a refresh can recover', async () => {
    mocks.get.mockRejectedValueOnce(new Error('Unavailable'))
    await act(async () => root.render(<Probe />))
    expect(result.autoScheduleError).toBe(true)
    await act(async () => result.refreshOccurrences())
    expect(result.autoScheduleError).toBe(false)
    expect(result.autoScheduleOccurrences).toEqual([occurrence])
})
