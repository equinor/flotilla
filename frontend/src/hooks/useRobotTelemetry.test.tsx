import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { describe, expect, test, vi, beforeEach, afterEach } from 'vitest'

class FakeHubConnection {
    handlers: Record<string, ((username: string, message: string) => void)[]> = {}
    state = 'Connected'

    on(event: string, handler: (username: string, message: string) => void) {
        this.handlers[event] = [...(this.handlers[event] ?? []), handler]
    }
    off(event: string, handler: (username: string, message: string) => void) {
        this.handlers[event] = (this.handlers[event] ?? []).filter((h) => h !== handler)
    }
    handlerCount(event: string) {
        return (this.handlers[event] ?? []).length
    }
    emit(event: string, message: string) {
        ;[...(this.handlers[event] ?? [])].forEach((h) => h('all', message))
    }
    start() {
        return Promise.resolve()
    }
    stop() {
        return Promise.resolve()
    }
    onclose() {}
    onreconnected() {}
    onreconnecting() {}
}

const connection = new FakeHubConnection()

vi.mock('@microsoft/signalr', () => ({
    HubConnectionBuilder: class {
        withUrl() {
            return this
        }
        withAutomaticReconnect() {
            return this
        }
        configureLogging() {
            return this
        }
        build() {
            return connection
        }
    },
    HttpTransportType: { WebSockets: 1, ServerSentEvents: 2, LongPolling: 4 },
    HubConnectionState: { Connected: 'Connected' },
    LogLevel: { Critical: 6 },
}))

vi.mock('@azure/msal-react', () => ({
    useMsal: () => ({ accounts: [{ username: 'test' }], inProgress: 'none' }),
}))

vi.mock('config', () => ({
    config: { BACKEND_API_SIGNALR_URL: 'http://localhost:8000/hub' },
}))

// The hook only uses useAssetContext in the sibling useAllRobotPosesTelemetry hook,
// but the module-level import has to resolve.
vi.mock('components/Contexts/AssetContext', () => ({
    useAssetContext: () => ({ enabledRobots: [] }),
}))

import { SignalRProvider } from 'components/Contexts/SignalRContext'
import { useRobotTelemetry } from './useRobotTelemetry'
import { RobotWithoutTelemetry } from 'models/Robot'

const TELEMETRY = 'Robot telemetry updated'

let container: HTMLDivElement
let root: Root

beforeEach(() => {
    connection.handlers = {}
    vi.useFakeTimers()
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    container.remove()
    vi.useRealTimers()
})

const mount = async (element: React.ReactNode) => {
    await act(async () => {
        root.render(element)
    })
}

const telemetryMessage = (robotId: string, name: string, value: unknown) =>
    JSON.stringify({ robotId, telemetryName: name, telemetryValue: value })

/** Renders the hook and exposes the battery level it reports. */
const BatteryReadout = ({ robot }: { robot: RobotWithoutTelemetry }) => {
    const { robotBatteryLevel } = useRobotTelemetry(robot)
    return <span data-testid="battery">{robotBatteryLevel ?? 'none'}</span>
}

const batteryText = () => container.querySelector('[data-testid="battery"]')?.textContent

describe('useRobotTelemetry', () => {
    test('subscribes once and reports the battery level for its own robot', async () => {
        await mount(
            <SignalRProvider>
                <BatteryReadout robot={{ id: 'robot-1' } as RobotWithoutTelemetry} />
            </SignalRProvider>
        )
        expect(connection.handlerCount(TELEMETRY)).toBe(1)

        act(() => connection.emit(TELEMETRY, telemetryMessage('robot-1', 'batteryLevel', 42)))
        expect(batteryText()).toBe('42')
    })

    test('ignores telemetry addressed to a different robot', async () => {
        await mount(
            <SignalRProvider>
                <BatteryReadout robot={{ id: 'robot-1' } as RobotWithoutTelemetry} />
            </SignalRProvider>
        )

        act(() => connection.emit(TELEMETRY, telemetryMessage('robot-2', 'batteryLevel', 99)))
        expect(batteryText()).toBe('none')
    })

    test('does not re-subscribe when the robot object identity changes but the id does not', async () => {
        // The bug this guards: the effect depended on the whole robot object, which is
        // replaced on every fetch, so it re-subscribed on every telemetry update.
        // Before the fix this reached 6 handlers after five updates.
        const Host = ({ robot }: { robot: RobotWithoutTelemetry }) => (
            <SignalRProvider>
                <BatteryReadout robot={robot} />
            </SignalRProvider>
        )

        await mount(<Host robot={{ id: 'robot-1', name: 'a' } as RobotWithoutTelemetry} />)
        expect(connection.handlerCount(TELEMETRY)).toBe(1)

        for (let i = 0; i < 5; i++) {
            await mount(<Host robot={{ id: 'robot-1', name: `a${i}` } as RobotWithoutTelemetry} />)
        }

        expect(connection.handlerCount(TELEMETRY)).toBe(1)
    })

    test('clears a stale battery reading after 30s of silence', async () => {
        await mount(
            <SignalRProvider>
                <BatteryReadout robot={{ id: 'robot-1' } as RobotWithoutTelemetry} />
            </SignalRProvider>
        )

        act(() => connection.emit(TELEMETRY, telemetryMessage('robot-1', 'batteryLevel', 42)))
        expect(batteryText()).toBe('42')

        await act(async () => {
            vi.advanceTimersByTime(30 * 1000)
        })
        expect(batteryText()).toBe('none')
    })

    test('a fresh reading cancels the pending 30s expiry', async () => {
        // This is what the plain `let` timer handles broke: clearTimeout received a
        // variable that had been re-created by the re-render, so the first reading's
        // expiry still fired and blanked out a value that had just been refreshed.
        await mount(
            <SignalRProvider>
                <BatteryReadout robot={{ id: 'robot-1' } as RobotWithoutTelemetry} />
            </SignalRProvider>
        )

        act(() => connection.emit(TELEMETRY, telemetryMessage('robot-1', 'batteryLevel', 42)))

        // 20s later a new reading arrives, which must reset the timer.
        await act(async () => {
            vi.advanceTimersByTime(20 * 1000)
        })
        act(() => connection.emit(TELEMETRY, telemetryMessage('robot-1', 'batteryLevel', 55)))
        expect(batteryText()).toBe('55')

        // The first reading's expiry would have fired here. It must not.
        await act(async () => {
            vi.advanceTimersByTime(15 * 1000)
        })
        expect(batteryText()).toBe('55')

        // The second reading's own expiry still works.
        await act(async () => {
            vi.advanceTimersByTime(15 * 1000)
        })
        expect(batteryText()).toBe('none')
    })
})
