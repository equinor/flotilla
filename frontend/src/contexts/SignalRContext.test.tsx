import { act, useEffect } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { describe, expect, test, vi, beforeEach, afterEach } from 'vitest'

/**
 * A stand-in for a SignalR HubConnection that records handlers exactly the way
 * the real client does, so we can assert how many are attached to each event.
 */
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

import { SignalRProvider, useSignalRContext } from './SignalRContext'
import { unsubscribeAll } from 'utils/signalR'

const EVENT = 'Mission run updated'

let container: HTMLDivElement
let root: Root

beforeEach(() => {
    connection.handlers = {}
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    container.remove()
})

const mount = async (element: React.ReactNode) => {
    await act(async () => {
        root.render(element)
    })
}

/** Mirrors how the real consumers subscribe: guard, subscribe, return cleanup. */
const Subscriber = ({ onMessage }: { onMessage?: (message: string) => void }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(EVENT, (_username, message) => onMessage?.(message))
    }, [registerEvent, connectionReady, onMessage])
    return null
}

describe('registerEvent', () => {
    test('attaches exactly one handler and removes it on unmount', async () => {
        await mount(
            <SignalRProvider>
                <Subscriber />
            </SignalRProvider>
        )
        expect(connection.handlerCount(EVENT)).toBe(1)

        await act(async () => {
            root.render(<></>)
        })
        expect(connection.handlerCount(EVENT)).toBe(0)
    })

    test('does not accumulate handlers when an ancestor of the provider re-renders', async () => {
        // The bug this guards: registerEvent used to be a new function on every
        // provider render, and it is listed in every consumer's effect dependencies,
        // so each ancestor re-render attached another handler that was never removed.
        // Before the fix this reached 6 handlers after five re-renders.
        const Ancestor = ({ tick }: { tick: number }) => (
            <SignalRProvider>
                <Subscriber />
                <span>{tick}</span>
            </SignalRProvider>
        )

        await mount(<Ancestor tick={0} />)
        expect(connection.handlerCount(EVENT)).toBe(1)

        // Re-render the tree above the provider now that the connection is ready.
        for (let i = 1; i <= 5; i++) {
            await mount(<Ancestor tick={i} />)
        }

        expect(connection.handlerCount(EVENT)).toBe(1)
    })

    test('does not accumulate handlers across repeated mounts and unmounts', async () => {
        // Mirrors navigating between missions: MissionPage subscribes on mount.
        for (let i = 0; i < 5; i++) {
            await mount(
                <SignalRProvider>
                    <Subscriber />
                </SignalRProvider>
            )
            expect(connection.handlerCount(EVENT)).toBe(1)
            await act(async () => {
                root.render(<></>)
            })
        }
        expect(connection.handlerCount(EVENT)).toBe(0)
    })

    test('a delivered message reaches the handler exactly once', async () => {
        const received: string[] = []
        await mount(
            <SignalRProvider>
                <Subscriber onMessage={(m) => received.push(m)} />
            </SignalRProvider>
        )

        act(() => connection.emit(EVENT, '{"id":"abc"}'))

        expect(received).toEqual(['{"id":"abc"}'])
    })

    test('messages with the literal string null are dropped', async () => {
        const received: string[] = []
        await mount(
            <SignalRProvider>
                <Subscriber onMessage={(m) => received.push(m)} />
            </SignalRProvider>
        )

        act(() => connection.emit(EVENT, 'null'))

        expect(received).toEqual([])
    })

    test('several subscribers on one event each receive the message once', async () => {
        const a: string[] = []
        const b: string[] = []
        await mount(
            <SignalRProvider>
                <Subscriber onMessage={(m) => a.push(m)} />
                <Subscriber onMessage={(m) => b.push(m)} />
            </SignalRProvider>
        )
        expect(connection.handlerCount(EVENT)).toBe(2)

        act(() => connection.emit(EVENT, 'x'))

        expect(a).toEqual(['x'])
        expect(b).toEqual(['x'])
    })
})

describe('unsubscribeAll', () => {
    test('invokes every unsubscribe function once', () => {
        const calls: string[] = []
        const cleanup = unsubscribeAll([() => calls.push('a'), () => calls.push('b'), () => calls.push('c')])

        expect(calls).toEqual([])
        cleanup()
        expect(calls).toEqual(['a', 'b', 'c'])
    })
})
