import { act, ReactNode } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { AutoScheduleOccurrence, AutoScheduleOccurrenceStatus } from 'models/AutoScheduleOccurrence'
import { LanguageProvider } from 'contexts/LanguageContext'
import { DashboardScheduledMissionsView } from 'pages/DashboardPage/DashboardScheduledMissionsView'
import { NextAutoScheduleMissionView } from 'pages/FrontPage/AutoScheduleSection/NextAutoScheduleMissionView'
import { SkipAutoMissionDialog } from 'pages/FrontPage/AutoScheduleSection/SkipAutoMissionDialog'
import { CalendarPro } from 'pages/FrontPage/AutoScheduleSection/AutoScheduleCalendar'
import { DaysOfWeek } from 'models/AutoScheduleFrequency'
import { BackendApi } from 'api/BackendApi'
import { BackendAPICaller } from 'api/ApiCaller'

const state = vi.hoisted(() => ({
    occurrences: [] as AutoScheduleOccurrence[],
    error: false,
    skip: vi.fn(),
    unskip: vi.fn(),
    update: vi.fn(),
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
        updateMissionDefinition: state.update,
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
    scheduledAt: '2026-10-16T23:00:00.123456Z',
    status: AutoScheduleOccurrenceStatus.Scheduled,
    skipId: null,
    mission: {
        id: 'mission-id',
        name: 'Inspection',
        installationCode: 'test',
        isAdHoc: false,
        tasks: [],
        autoScheduleFrequency: {
            schedulingTimesCETperWeek: [{ dayOfWeek: DaysOfWeek.Saturday, timeOfDay: '01:00:00.123456' }],
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
    state.update.mockResolvedValue(undefined)
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})
afterEach(() => {
    vi.restoreAllMocks()
    act(() => root.unmount())
    container.remove()
    window.localStorage.clear()
    vi.unstubAllGlobals()
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal')
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'close')
})

test('skip API preserves fractional timestamps and undo deletes by opaque skip ID without a body', async () => {
    const caller = new BackendAPICaller(async () => 'test', 'http://localhost')
    const put = vi.spyOn(caller, 'PUT').mockResolvedValue({ content: '', headers: new Headers() })
    const remove = vi.spyOn(caller, 'DELETE').mockResolvedValue({ content: '', headers: new Headers() })
    const api = new BackendApi(caller)
    const scheduledAt = '2026-10-17T01:00:00.123456+02:00'
    await api.skipAutoScheduledMission('mission-id', scheduledAt)
    const skipId = '9007199254740993'
    await api.unskipAutoScheduledMission('mission-id', skipId)
    expect(put).toHaveBeenCalledWith('missions/definitions/mission-id/skip-auto-mission', { scheduledAt })
    expect(remove).toHaveBeenCalledExactlyOnceWith(`missions/definitions/mission-id/auto-schedule-skips/${skipId}`)
})

test('dashboard shows all planned occurrences, including after midnight, without skip controls', () => {
    state.occurrences = [
        ...Array.from({ length: 5 }, (_, i) => ({ ...tomorrow, mission: { ...tomorrow.mission, id: `mission-${i}` } })),
        { ...tomorrow, status: AutoScheduleOccurrenceStatus.Skipped, skipId: '42' },
    ]
    render(<DashboardScheduledMissionsView />)
    expect(container.textContent?.match(/Inspection/g)).toHaveLength(5)
    expect(container.textContent).toContain('17/10')
    expect(container.querySelector('button')).toBeNull()
})

test.each([AutoScheduleOccurrenceStatus.Scheduled, AutoScheduleOccurrenceStatus.Skipped])(
    'calendar changes tomorrow %s occurrence without losing timestamp precision',
    async (status) => {
        const label = status === AutoScheduleOccurrenceStatus.Skipped ? 'Undo skip' : 'Skip'
        state.occurrences = [
            { ...tomorrow, status, skipId: status === AutoScheduleOccurrenceStatus.Skipped ? '42' : null },
        ]
        render(<CalendarPro />)
        if (status === AutoScheduleOccurrenceStatus.Skipped) expect(container.textContent).toContain('(Skipped)')
        act(() => button(label).click())
        expect(document.body.textContent).toContain('17/10')
        const confirm = button(label, 'dialog button')
        expect(confirm.disabled).toBe(false)
        await act(async () => confirm.click())
        expect(
            status === AutoScheduleOccurrenceStatus.Skipped ? state.unskip : state.skip
        ).toHaveBeenCalledExactlyOnceWith(
            'mission-id',
            status === AutoScheduleOccurrenceStatus.Skipped ? '42' : tomorrow.scheduledAt
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

test.each([AutoScheduleOccurrenceStatus.Scheduled, AutoScheduleOccurrenceStatus.Skipped])(
    'a rejected change to %s keeps the dialog open and reports an error',
    async (status) => {
        const occurrence = {
            ...tomorrow,
            status,
            skipId: status === AutoScheduleOccurrenceStatus.Skipped ? '42' : null,
        }
        state.occurrences = [occurrence]
        const request = status === AutoScheduleOccurrenceStatus.Skipped ? state.unskip : state.skip
        request.mockRejectedValue(new Error('Conflict'))
        const close = vi.fn()
        render(<SkipAutoMissionDialog occurrence={occurrence} onClose={close} />)
        await act(async () => button(status === AutoScheduleOccurrenceStatus.Skipped ? 'Undo skip' : 'Skip').click())
        expect(state.banner).toHaveBeenCalledWith(
            expect.stringContaining(
                status === AutoScheduleOccurrenceStatus.Skipped ? 'Unable to undo' : 'Unable to skip'
            ),
            'error'
        )
        expect(close).not.toHaveBeenCalled()
        expect(state.refresh).toHaveBeenCalledOnce()
    }
)

test('expanded list includes skipped missions and undo restores only the selected occurrence', async () => {
    const skipped = {
        ...tomorrow,
        mission: { ...tomorrow.mission, id: 'skipped-mission' },
        status: AutoScheduleOccurrenceStatus.Skipped,
        skipId: '42',
    }
    state.occurrences = [skipped, tomorrow]
    state.unskip.mockImplementation(async () => {
        state.occurrences = [tomorrow]
    })
    render(<NextAutoScheduleMissionView />)
    act(() => button('Show more').click())
    expect(container.textContent?.match(/Inspection/g)).toHaveLength(2)
    expect(container.textContent).toContain('Skipped')
    expect(container.textContent).toContain('17/10')
    expect(button('Skip').disabled).toBe(false)
    expect(button('Undo skip').disabled).toBe(false)
    act(() => button('Undo skip').click())
    expect(document.body.textContent).toContain('Restore Inspection scheduled for 17/10')
    await act(async () => button('Undo skip', 'dialog button').click())
    expect(state.unskip).toHaveBeenCalledExactlyOnceWith('skipped-mission', skipped.skipId)
    expect(state.skip).not.toHaveBeenCalled()
    expect(state.refresh).toHaveBeenCalledOnce()
    render(<NextAutoScheduleMissionView />)
    expect(container.textContent).not.toContain('Skipped')
    expect(button('Skip').disabled).toBe(false)
})

test.each(['missing', 'restored', 'replaced', 'expired', 'error'])(
    'undo confirmation rejects a %s occurrence',
    (reason) => {
        const skipped = { ...tomorrow, status: AutoScheduleOccurrenceStatus.Skipped, skipId: '42' }
        state.occurrences =
            reason === 'missing'
                ? []
                : reason === 'restored'
                  ? [tomorrow]
                  : [
                        {
                            ...skipped,
                            skipId: reason === 'replaced' ? '43' : skipped.skipId,
                            scheduledAt: reason === 'expired' ? '2026-10-16T20:00:00Z' : skipped.scheduledAt,
                        },
                    ]
        state.error = reason === 'error'
        render(<SkipAutoMissionDialog occurrence={skipped} onClose={vi.fn()} />)
        expect(button('Undo skip').disabled).toBe(true)
        expect(document.body.textContent).toContain('This skip can no longer be undone.')
    }
)

test('an open skip confirmation stays a skip and becomes disabled after a live update', () => {
    render(<NextAutoScheduleMissionView />)
    act(() => button('Skip').click())
    state.occurrences = [{ ...tomorrow, status: AutoScheduleOccurrenceStatus.Skipped, skipId: '42' }]
    render(<NextAutoScheduleMissionView />)
    const confirm = button('Skip', 'dialog button')
    expect(confirm.disabled).toBe(true)
    act(() => confirm.click())
    expect(state.skip).not.toHaveBeenCalled()
    expect(state.unskip).not.toHaveBeenCalled()
})

test('name-only edits do not resend a legacy fractional schedule', async () => {
    const { MissionNameEditDialog } = await vi.importActual<typeof import('components/Dialogs/MissionEditDialog')>(
        'components/Dialogs/MissionEditDialog'
    )
    render(<MissionNameEditDialog mission={tomorrow.mission} isOpen onClose={vi.fn()} />)
    await act(async () => button('Update').click())
    expect(state.update).toHaveBeenCalledExactlyOnceWith(tomorrow.mission.id, { name: tomorrow.mission.name })
})
