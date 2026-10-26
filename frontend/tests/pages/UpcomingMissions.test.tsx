import { act, ReactNode } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { AutoScheduleOccurrence } from 'models/AutoScheduleOccurrence'
import { LanguageProvider } from 'contexts/LanguageContext'
import { DashboardScheduledMissionsView } from 'pages/DashboardPage/DashboardScheduledMissionsView'
import { NextAutoScheduleMissionView } from 'pages/FrontPage/AutoScheduleSection/NextAutoScheduleMissionView'
import { SkipAutoMissionDialog } from 'pages/FrontPage/AutoScheduleSection/SkipAutoMissionDialog'
import { CalendarPro } from 'pages/FrontPage/AutoScheduleSection/AutoScheduleCalendar'
import { DaysOfWeek } from 'models/AutoScheduleFrequency'

const state = vi.hoisted(() => ({
    occurrences: [] as AutoScheduleOccurrence[],
    error: false,
    skip: vi.fn(),
    unskip: vi.fn(),
    refresh: vi.fn(),
    banner: vi.fn(),
}))

vi.mock('hooks/useNow', () => ({ useNow: () => new Date('2026-10-16T20:00:00Z') }))
vi.mock('contexts/MissionDefinitionsContext', () => ({
    useMissionDefinitionsContext: () => ({
        autoScheduleOccurrences: state.occurrences,
        missionDefinitions: [tomorrow.mission],
        autoScheduleLoading: false,
        autoScheduleError: state.error,
        refreshOccurrences: state.refresh,
    }),
}))
vi.mock('api/UseBackendApi', () => ({
    useBackendApi: () => ({
        skipAutoScheduledMission: state.skip,
        unskipAutoScheduledMission: state.unskip,
    }),
}))
vi.mock('contexts/AlertContext', () => ({ useAlertContext: () => ({ setBanner: state.banner }) }))
vi.mock('components/Dialogs/MissionEditDialog', () => ({ MissionSchedulingEditDialog: () => null }))
vi.mock('react-router', () => ({ Link: 'a', useNavigate: () => vi.fn() }))
vi.mock('contexts/InstallationContext', async () => {
    const { createContext } = await import('react')
    return { InstallationContext: createContext({ installation: { installationCode: 'test' } }) }
})

const tomorrow: AutoScheduleOccurrence = {
    scheduleKey: 'tomorrow-key',
    scheduledAt: '2026-10-16T23:00:00Z',
    status: 'Scheduled',
    mission: {
        id: 'mission-id',
        name: 'Inspection',
        installationCode: 'test',
        isAdHoc: false,
        tasks: [],
        autoScheduleFrequency: {
            schedulingTimesCETperWeek: [{ dayOfWeek: DaysOfWeek.Saturday, timeOfDay: '01:00:00' }],
        },
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

const render = (view: ReactNode) => act(() => root.render(<LanguageProvider>{view}</LanguageProvider>))
const button = (label: string, selector = 'button') => {
    const match = [...document.querySelectorAll<HTMLButtonElement>(selector)].find(
        (element) => element.textContent === label
    )
    expect(match).toBeDefined()
    return match!
}

beforeEach(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', { configurable: true, value: vi.fn() })
    Object.defineProperty(HTMLDialogElement.prototype, 'close', { configurable: true, value: vi.fn() })
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    window.localStorage.setItem('flotilla-language', 'en')
    state.occurrences = [tomorrow]
    state.error = false
    vi.clearAllMocks()
    state.skip.mockResolvedValue(undefined)
    state.unskip.mockResolvedValue(undefined)
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
    state.occurrences = Array.from({ length: 5 }, (_, i) => ({ ...tomorrow, scheduleKey: `schedule-key-${i}` }))
    render(<DashboardScheduledMissionsView />)
    expect(container.textContent?.match(/Inspection/g)).toHaveLength(5)
    expect(container.textContent).toContain('17/10')
    expect(container.querySelector('button')).toBeNull()
})

test('mission-control list offers Skip for tomorrow', () => {
    render(<NextAutoScheduleMissionView />)
    expect(button('Skip').disabled).toBe(false)
    expect(container.textContent).toContain('17/10')
})

test.each(['Scheduled', 'Skipped'] as const)(
    'calendar changes tomorrow %s occurrence by its exact schedule key',
    async (status) => {
        const label = status === 'Skipped' ? 'Undo skip' : 'Skip'
        state.occurrences = [{ ...tomorrow, status }]
        render(<CalendarPro />)
        if (status === 'Skipped') expect(container.textContent).toContain('(Skipped)')
        act(() => button(label).click())
        expect(document.body.textContent).toContain('17/10')
        const confirm = button(label, 'dialog button')
        expect(confirm.disabled).toBe(false)
        await act(async () => confirm.click())
        expect(status === 'Skipped' ? state.unskip : state.skip).toHaveBeenCalledExactlyOnceWith(
            'mission-id',
            'tomorrow-key'
        )
        expect(state.refresh).toHaveBeenCalledOnce()
    }
)

test('retrieval errors are not presented as an empty successful schedule', () => {
    state.error = true
    render(<DashboardScheduledMissionsView />)
    expect(container.textContent).toContain('Failed to retrieve scheduled missions')
    expect(container.textContent).not.toContain('There are no scheduled missions')
})

test('stale occurrences cannot be confirmed', () => {
    state.occurrences = []
    render(<SkipAutoMissionDialog occurrence={tomorrow} onClose={vi.fn()} />)
    expect(button('Skip').disabled).toBe(true)
})

test.each(['Scheduled', 'Skipped'] as const)(
    'a rejected change to %s keeps the dialog open and reports an error',
    async (status) => {
        const occurrence = { ...tomorrow, status }
        state.occurrences = [occurrence]
        const request = status === 'Skipped' ? state.unskip : state.skip
        request.mockRejectedValue(new Error('Conflict'))
        const close = vi.fn()
        render(<SkipAutoMissionDialog occurrence={occurrence} onClose={close} />)
        await act(async () => button(status === 'Skipped' ? 'Undo skip' : 'Skip').click())
        expect(state.banner).toHaveBeenCalledWith(
            expect.stringContaining(status === 'Skipped' ? 'Unable to undo' : 'Unable to skip'),
            'error'
        )
        expect(close).not.toHaveBeenCalled()
        expect(state.refresh).toHaveBeenCalledOnce()
    }
)

test('upcoming list displays skipped missions and undo submits only the selected dated schedule key', async () => {
    const skipped = { ...tomorrow, status: 'Skipped' as const }
    state.occurrences = [skipped]
    state.unskip.mockImplementation(async () => {
        state.occurrences = [tomorrow]
    })
    render(<NextAutoScheduleMissionView />)
    expect(container.textContent).toContain('Skipped')
    expect(container.textContent).toContain('17/10')
    expect(button('Undo skip').disabled).toBe(false)
    act(() => button('Undo skip').click())
    expect(document.body.textContent).toContain('Restore Inspection scheduled for 17/10')
    await act(async () => button('Undo skip', 'dialog button').click())
    expect(state.unskip).toHaveBeenCalledExactlyOnceWith('mission-id', 'tomorrow-key')
    expect(state.skip).not.toHaveBeenCalled()
    expect(state.refresh).toHaveBeenCalledOnce()
    render(<NextAutoScheduleMissionView />)
    expect(container.textContent).not.toContain('Skipped')
    expect(button('Skip').disabled).toBe(false)
})

test('expanded upcoming list keeps both skipped and planned missions visible', () => {
    state.occurrences = [{ ...tomorrow, scheduleKey: 'skipped-key', status: 'Skipped' }, tomorrow]
    render(<NextAutoScheduleMissionView />)
    act(() => button('Show more').click())
    expect(container.textContent?.match(/Inspection/g)).toHaveLength(2)
    expect(container.textContent).toContain('Skipped')
    expect(container.textContent).toContain('Undo skip')
    expect(button('Skip').disabled).toBe(false)
})

test.each(['restored', 'expired', 'error'])('undo confirmation rejects a %s occurrence', (reason) => {
    const skipped = { ...tomorrow, status: 'Skipped' as const }
    state.occurrences =
        reason === 'restored'
            ? [tomorrow]
            : [{ ...skipped, scheduledAt: reason === 'expired' ? '2026-10-16T20:00:00Z' : skipped.scheduledAt }]
    state.error = reason === 'error'
    render(<SkipAutoMissionDialog occurrence={skipped} onClose={vi.fn()} />)
    expect(button('Undo skip').disabled).toBe(true)
    expect(document.body.textContent).toContain('This skip can no longer be undone.')
})

test('dashboard still excludes skipped entries and has no undo control', () => {
    state.occurrences = [{ ...tomorrow, status: 'Skipped' }]
    render(<DashboardScheduledMissionsView />)
    expect(container.textContent).toContain('There are no scheduled missions')
    expect(container.querySelector('button')).toBeNull()
})
