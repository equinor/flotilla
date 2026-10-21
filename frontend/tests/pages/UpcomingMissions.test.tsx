import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { ScheduledMissionRun } from 'models/ScheduledMissionRun'
import { LanguageProvider } from 'contexts/LanguageContext'
import { DashboardScheduledMissionsView } from 'pages/DashboardPage/DashboardScheduledMissionsView'
import { NextAutoScheduleMissionView } from 'pages/FrontPage/AutoScheduleSection/NextAutoScheduleMissionView'
import { SkipAutoMissionDialog } from 'pages/FrontPage/AutoScheduleSection/SkipAutoMissionDialog'

const state = vi.hoisted(() => ({
    occurrences: [] as ScheduledMissionRun[],
    error: false,
    skip: vi.fn(),
    refresh: vi.fn(),
    banner: vi.fn(),
}))

vi.mock('hooks/useNow', () => ({ useNow: () => new Date('2026-10-16T20:00:00Z') }))
vi.mock('contexts/MissionDefinitionsContext', () => ({
    useMissionDefinitionsContext: () => ({
        scheduledMissionRuns: state.occurrences,
        autoScheduleLoading: false,
        autoScheduleError: state.error,
        refreshScheduledRuns: state.refresh,
    }),
}))
vi.mock('api/UseBackendApi', () => ({ useBackendApi: () => ({ skipAutoScheduledMission: state.skip }) }))
vi.mock('contexts/AlertContext', () => ({ useAlertContext: () => ({ setBanner: state.banner }) }))
vi.mock('components/Dialogs/MissionEditDialog', () => ({ MissionSchedulingEditDialog: () => null }))
vi.mock('react-router', () => ({ Link: 'a' }))
vi.mock('contexts/InstallationContext', async () => {
    const { createContext } = await import('react')
    return { InstallationContext: createContext({ installation: { installationCode: 'test' } }) }
})

const tomorrow: ScheduledMissionRun = {
    id: 'tomorrow-id',
    scheduledAt: '2026-10-16T23:00:00Z',
    status: 'Scheduled',
    mission: {
        id: 'mission-id',
        name: 'Inspection',
        installationCode: 'test',
        isAdHoc: false,
        tasks: [],
        inspectionArea: {
            id: 'area',
            inspectionAreaName: 'Area',
            plantCode: 'plant',
            plantName: 'Plant',
            installationCode: 'test',
        },
    },
}
let root: Root
let container: HTMLDivElement

beforeEach(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', { configurable: true, value: vi.fn() })
    Object.defineProperty(HTMLDialogElement.prototype, 'close', { configurable: true, value: vi.fn() })
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    window.localStorage.setItem('flotilla-language', 'en')
    state.occurrences = [tomorrow]
    state.error = false
    vi.clearAllMocks()
    state.skip.mockResolvedValue(undefined)
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})
afterEach(() => {
    act(() => root.unmount())
    container.remove()
    window.localStorage.clear()
    vi.unstubAllGlobals()
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal')
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'close')
})

test('dashboard shows all occurrences, including after midnight, without skip controls', () => {
    state.occurrences = Array.from({ length: 5 }, (_, i) => ({ ...tomorrow, id: `occurrence-${i}` }))
    act(() =>
        root.render(
            <LanguageProvider>
                <DashboardScheduledMissionsView />
            </LanguageProvider>
        )
    )
    expect(container.textContent?.match(/Inspection/g)).toHaveLength(5)
    expect(container.textContent).toContain('17/10')
    expect(container.querySelector('button')).toBeNull()
})

test('mission-control list offers Skip for tomorrow', () => {
    act(() =>
        root.render(
            <LanguageProvider>
                <NextAutoScheduleMissionView />
            </LanguageProvider>
        )
    )
    const skip = [...container.querySelectorAll('button')].find((button) => button.textContent === 'Skip')
    expect(skip?.disabled).toBe(false)
    expect(container.textContent).toContain('17/10')
})

test('retrieval errors are not presented as an empty successful schedule', () => {
    state.error = true
    act(() =>
        root.render(
            <LanguageProvider>
                <DashboardScheduledMissionsView />
            </LanguageProvider>
        )
    )
    expect(container.textContent).toContain('Failed to retrieve scheduled missions')
    expect(container.textContent).not.toContain('There are no scheduled missions')
})

test('confirmation submits the exact occurrence ID and actual date', async () => {
    const close = vi.fn()
    act(() =>
        root.render(
            <LanguageProvider>
                <SkipAutoMissionDialog occurrence={tomorrow} onClose={close} />
            </LanguageProvider>
        )
    )
    expect(document.body.textContent).toContain('17/10')
    const skip = [...document.querySelectorAll('button')].find((button) => button.textContent === 'Skip')
    await act(async () => skip?.click())
    expect(state.skip).toHaveBeenCalledWith('mission-id', 'tomorrow-id')
    expect(close).toHaveBeenCalledOnce()
    expect(state.refresh).toHaveBeenCalledOnce()
})

test('stale occurrences cannot be confirmed', () => {
    state.occurrences = []
    act(() =>
        root.render(
            <LanguageProvider>
                <SkipAutoMissionDialog occurrence={tomorrow} onClose={vi.fn()} />
            </LanguageProvider>
        )
    )
    const skip = [...document.querySelectorAll('button')].find((button) => button.textContent === 'Skip')
    expect(skip?.disabled).toBe(true)
})

test('a rejected skip displays an error and keeps the dialog open', async () => {
    state.skip.mockRejectedValue(new Error('Conflict'))
    const close = vi.fn()
    act(() =>
        root.render(
            <LanguageProvider>
                <SkipAutoMissionDialog occurrence={tomorrow} onClose={close} />
            </LanguageProvider>
        )
    )
    const skip = [...document.querySelectorAll('button')].find((button) => button.textContent === 'Skip')
    await act(async () => skip?.click())
    expect(state.banner).toHaveBeenCalledWith(expect.stringContaining('Unable to skip'), 'error')
    expect(close).not.toHaveBeenCalled()
})
