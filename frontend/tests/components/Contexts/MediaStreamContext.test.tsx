import { act, StrictMode } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import { RoomEvent } from 'livekit-client'
import { MediaConnectionType, MediaStreamConfig } from 'models/VideoStream'
import { MediaStreamProvider } from 'contexts/MediaStreamContext'
import { VideoStreamWindow } from 'pages/MissionPage/VideoStream/VideoStreamWindow'
import { LanguageProvider } from 'contexts/LanguageContext'

const { rooms, MockRoom, backendApi, connectRoom } = vi.hoisted(() => {
    const connectRoom = vi.fn<() => Promise<void>>()
    class MockRoom {
        handlers: Record<string, ((...args: unknown[]) => void)[]> = {}
        connect = vi.fn(() => connectRoom())
        disconnect = vi.fn(() => Promise.resolve())
        removeAllListeners = vi.fn(() => {
            this.handlers = {}
        })
        on(event: string, handler: (...args: unknown[]) => void) {
            this.handlers[event] = [...(this.handlers[event] ?? []), handler]
            return this
        }
        emit(event: string, ...args: unknown[]) {
            this.handlers[event]?.forEach((handler) => handler(...args))
        }
    }
    return {
        MockRoom,
        connectRoom,
        rooms: [] as MockRoom[],
        backendApi: {
            getRobotMediaConfig: vi.fn<(robotId: string) => Promise<MediaStreamConfig | null | undefined>>(),
        },
    }
})

vi.mock('livekit-client', async (importOriginal) => ({
    ...(await importOriginal<typeof import('livekit-client')>()),
    Room: class extends MockRoom {
        constructor() {
            super()
            rooms.push(this)
        }
    },
}))
vi.mock('api/UseBackendApi', () => ({ useBackendApi: () => backendApi }))
vi.mock('pages/MissionPage/VideoStream/VideoStreamCards', () => ({
    VideoStreamCard: () => <div data-testid="camera" />,
}))

const config: MediaStreamConfig = {
    robotId: 'robot-1',
    url: 'wss://livekit.example',
    token: 'fresh-token',
    mediaConnectionType: MediaConnectionType.LiveKit,
}
let root: Root
let container: HTMLDivElement

const mount = async (children = <VideoStreamWindow robotId="robot-1" />, strict = false) => {
    const element = (
        <LanguageProvider>
            <MediaStreamProvider>{children}</MediaStreamProvider>
        </LanguageProvider>
    )
    await act(async () => root.render(strict ? <StrictMode>{element}</StrictMode> : element))
}
const advance = async (milliseconds: number) => {
    await act(async () => vi.advanceTimersByTimeAsync(milliseconds))
}
const subscribe = (
    room: InstanceType<typeof MockRoom>,
    sid = 'publication-1',
    id = 'browser-track-1',
    kind = 'video'
) => {
    act(() => room.emit(RoomEvent.TrackSubscribed, { kind, mediaStreamTrack: { id } }, { trackSid: sid }))
}
const status = () => container.querySelector('[role="status"]')?.textContent
const cameras = () => container.querySelectorAll('[data-testid="camera"]').length
const deferred = <T,>() => {
    let resolve!: (value: T) => void
    const promise = new Promise<T>((resolver) => {
        resolve = resolver
    })
    return { promise, resolve }
}

beforeEach(() => {
    vi.useFakeTimers()
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    vi.stubGlobal('MediaStream', class {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})
    window.localStorage.clear()
    window.localStorage.setItem('flotilla-language', 'en')
    rooms.length = 0
    connectRoom.mockReset().mockResolvedValue()
    backendApi.getRobotMediaConfig.mockReset().mockResolvedValue(config)
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    container.remove()
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
    vi.useRealTimers()
})

describe('livestream viewing and recovery', () => {
    test('activates the backend even with a valid cached token and does not persist new credentials', async () => {
        const cached = JSON.stringify({ 'robot-1': { ...config, token: 'valid-cached-token' } })
        window.localStorage.setItem('mediaConfigs', cached)
        await mount()
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledExactlyOnceWith('robot-1')
        expect(rooms[0].connect).toHaveBeenCalledWith(config.url, 'fresh-token')
        expect(window.localStorage.getItem('mediaConfigs')).toBe(cached)
        expect(status()).toBe('Connecting')
        subscribe(rooms[0])
        expect(cameras()).toBe(1)
        expect(status()).toBeUndefined()
        await advance(120_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(1)
    })

    test('ignores malformed legacy credential storage', async () => {
        window.localStorage.setItem('mediaConfigs', '{invalid')
        await mount()
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(1)
    })

    test('a connected room without video has three bounded attempts, then a manual retry', async () => {
        await mount()
        subscribe(rooms[0], 'audio', 'audio', 'audio')
        expect(cameras()).toBe(0)
        await advance(30_000)
        expect(status()).toBe('Reconnecting')
        expect(rooms[0].disconnect).toHaveBeenCalledOnce()
        await advance(2_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(2)
        await advance(30_000)
        await advance(4_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(3)
        await advance(30_000)
        expect(status()).toBe('Stream unavailable')
        await advance(3_600_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(3)
        expect(vi.getTimerCount()).toBe(0)
        await mount()
        expect(status()).toBe('Stream unavailable')
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(3)
        await act(async () => {
            container.querySelector('button')!.click()
            container.querySelector('button')!.click()
        })
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(4)
        expect(status()).toBe('Connecting')
        subscribe(rooms[3])
        expect(cameras()).toBe(1)
    })

    test('removes tracks by publication SID, preserves other cameras, and recovers when all disappear', async () => {
        await mount()
        subscribe(rooms[0], 'sid-1', 'browser-id-1')
        subscribe(rooms[0], 'sid-2', 'browser-id-2')
        subscribe(rooms[0], 'sid-2', 'browser-id-2')
        expect(cameras()).toBe(2)
        act(() => rooms[0].emit(RoomEvent.TrackUnpublished, { trackSid: 'sid-1' }))
        expect(cameras()).toBe(1)
        await advance(35_000)
        expect(rooms).toHaveLength(1)
        act(() => rooms[0].emit(RoomEvent.TrackUnsubscribed, {}, { trackSid: 'sid-2' }))
        expect(cameras()).toBe(0)
        expect(status()).toBe('Reconnecting')
        await advance(32_000)
        expect(rooms).toHaveLength(2)
        subscribe(rooms[1])
        expect(cameras()).toBe(1)
        expect(status()).toBeUndefined()
    })

    test('a replacement track within the deadline avoids reconnecting the room', async () => {
        await mount()
        subscribe(rooms[0])
        act(() => rooms[0].emit(RoomEvent.TrackUnpublished, { trackSid: 'publication-1' }))
        await advance(20_000)
        subscribe(rooms[0], 'replacement')
        await advance(40_000)
        expect(rooms).toHaveLength(1)
        expect(cameras()).toBe(1)
    })

    test('disconnect clears stale cameras and refreshes activation with backoff', async () => {
        await mount()
        subscribe(rooms[0])
        act(() => rooms[0].emit(RoomEvent.Disconnected))
        expect(cameras()).toBe(0)
        expect(status()).toBe('Reconnecting')
        await advance(2_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(2)
        subscribe(rooms[1])
        expect(cameras()).toBe(1)
    })

    test.each([RoomEvent.Reconnecting, RoomEvent.SignalReconnecting])(
        '%s can recover without activation, but cannot hang indefinitely',
        async (event) => {
            await mount()
            subscribe(rooms[0])
            act(() => rooms[0].emit(event))
            expect(status()).toBe('Reconnecting')
            expect(cameras()).toBe(1)
            await advance(10_000)
            act(() => rooms[0].emit(RoomEvent.Reconnected))
            await advance(40_000)
            expect(status()).toBeUndefined()
            expect(rooms).toHaveLength(1)
            act(() => rooms[0].emit(event))
            await advance(32_000)
            expect(rooms).toHaveLength(2)
        }
    )

    test('bounds the total automatic attempts even if tracks briefly return between disconnects', async () => {
        await mount()
        for (let index = 0; index < 3; index++) {
            subscribe(rooms[index])
            act(() => rooms[index].emit(RoomEvent.Disconnected))
            await advance(4_000)
        }
        expect(status()).toBe('Stream unavailable')
        await advance(120_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(3)
    })

    test('repeated reconnect events preserve the original recovery deadline', async () => {
        await mount()
        subscribe(rooms[0])
        act(() => rooms[0].emit(RoomEvent.Reconnecting))
        await advance(20_000)
        act(() => rooms[0].emit(RoomEvent.SignalReconnecting))
        await advance(10_000)
        expect(rooms[0].disconnect).toHaveBeenCalledOnce()
        await advance(2_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(2)
    })

    test('stable video replenishes the retry budget for a later independent outage', async () => {
        await mount()
        await advance(66_000)
        expect(rooms).toHaveLength(3)
        subscribe(rooms[2])
        await advance(30_000)
        act(() => rooms[2].emit(RoomEvent.Disconnected))
        await advance(2_000)
        expect(rooms).toHaveLength(4)
        expect(status()).toBe('Reconnecting')
        await advance(66_000)
        expect(rooms).toHaveLength(6)
        subscribe(rooms[5])
        expect(cameras()).toBe(1)
        expect(status()).toBeUndefined()
    })

    test('signaling interruptions cannot replenish the stable-video retry budget', async () => {
        await mount()
        for (let index = 0; index < 3; index++) {
            subscribe(rooms[index])
            await advance(20_000)
            act(() => rooms[index].emit(RoomEvent.SignalReconnecting))
            await advance(15_000)
            act(() => rooms[index].emit(RoomEvent.Disconnected))
            await advance(4_000)
        }
        expect(status()).toBe('Stream unavailable')
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(3)
    })

    test('late config responses from timed-out attempts cannot create a superseded room', async () => {
        const pending = deferred<MediaStreamConfig>()
        backendApi.getRobotMediaConfig.mockReturnValueOnce(pending.promise)
        await mount()
        await advance(32_000)
        expect(rooms).toHaveLength(1)
        subscribe(rooms[0])
        await act(async () => pending.resolve({ ...config, token: 'obsolete' }))
        expect(rooms).toHaveLength(1)
        expect(cameras()).toBe(1)
    })

    test('leaving the camera view ignores pending config and re-entry activates again', async () => {
        const pending = deferred<MediaStreamConfig>()
        backendApi.getRobotMediaConfig.mockReturnValueOnce(pending.promise)
        await mount()
        await mount(<></>)
        await mount()
        subscribe(rooms[0])
        await act(async () => pending.resolve(config))
        expect(rooms).toHaveLength(1)
        expect(cameras()).toBe(1)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(2)
    })

    test('late connect completion disconnects the superseded room and ignores its events', async () => {
        const connecting = deferred<void>()
        connectRoom.mockReturnValueOnce(connecting.promise)
        await mount()
        const lateTrackHandler = rooms[0].handlers[RoomEvent.TrackSubscribed][0]
        await advance(32_000)
        subscribe(rooms[1])
        expect(rooms[0].disconnect).toHaveBeenCalledOnce()
        expect(rooms[0].handlers).toEqual({})
        await act(async () => connecting.resolve())
        act(() => lateTrackHandler({ kind: 'video', mediaStreamTrack: { id: 'obsolete' } }, { trackSid: 'obsolete' }))
        expect(rooms[0].disconnect).toHaveBeenCalledTimes(2)
        expect(cameras()).toBe(1)
        await advance(30_000)
        expect(vi.getTimerCount()).toBe(0)
    })

    test('a rejected room connection uses bounded retries and surfaces failure', async () => {
        connectRoom.mockRejectedValue(new Error('Connection rejected'))
        await mount()
        await advance(6_000)
        expect(status()).toBe('Stream unavailable')
        expect(rooms).toHaveLength(3)
        rooms.forEach((room) => expect(room.disconnect).toHaveBeenCalledOnce())
        expect(vi.getTimerCount()).toBe(0)
    })

    test('switching robot releases the old room and activates only the new robot', async () => {
        backendApi.getRobotMediaConfig.mockImplementation(async (robotId) => ({ ...config, robotId }))
        await mount()
        subscribe(rooms[0])
        await mount(<VideoStreamWindow robotId="robot-2" />)
        expect(rooms[0].disconnect).toHaveBeenCalledOnce()
        expect(cameras()).toBe(0)
        expect(backendApi.getRobotMediaConfig).toHaveBeenLastCalledWith('robot-2')
        subscribe(rooms[1])
        expect(cameras()).toBe(1)
    })

    test('concurrent viewers share one room and release it only when the last viewer leaves', async () => {
        await mount(
            <>
                <VideoStreamWindow robotId="robot-1" />
                <VideoStreamWindow robotId="robot-1" />
            </>
        )
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(1)
        expect(rooms).toHaveLength(1)
        await mount(
            <>
                <VideoStreamWindow robotId="robot-1" />
            </>
        )
        expect(rooms[0].disconnect).not.toHaveBeenCalled()
        await mount(<></>)
        expect(rooms[0].disconnect).toHaveBeenCalledOnce()
        await advance(0)
        expect(vi.getTimerCount()).toBe(0)
    })

    test('StrictMode replay makes one activation and provider cleanup removes rooms and timers', async () => {
        await mount(<VideoStreamWindow robotId="robot-1" />, true)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(1)
        expect(rooms).toHaveLength(1)
        subscribe(rooms[0])
        await act(async () => root.render(<></>))
        expect(rooms[0].disconnect).toHaveBeenCalledOnce()
        expect(rooms[0].handlers).toEqual({})
        await advance(0)
        expect(vi.getTimerCount()).toBe(0)
        await advance(120_000)
        expect(rooms).toHaveLength(1)
    })

    test('unmount during backoff cancels the next activation request', async () => {
        await mount()
        act(() => rooms[0].emit(RoomEvent.Disconnected))
        await act(async () => root.render(<></>))
        await advance(120_000)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(1)
        expect(vi.getTimerCount()).toBe(0)
    })

    test('provider unmount ignores pending activation responses', async () => {
        const pending = deferred<MediaStreamConfig>()
        backendApi.getRobotMediaConfig.mockReturnValueOnce(pending.promise)
        await mount()
        await act(async () => root.render(<></>))
        await act(async () => pending.resolve(config))
        await advance(120_000)
        expect(rooms).toHaveLength(0)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(1)
        expect(vi.getTimerCount()).toBe(0)
    })

    test.each(['missing', 'rejected', 'wrong-robot'])('surfaces %s config after finite retries', async (failure) => {
        if (failure === 'rejected') backendApi.getRobotMediaConfig.mockRejectedValue(new Error('Unavailable'))
        else
            backendApi.getRobotMediaConfig.mockResolvedValue(
                failure === 'missing' ? null : { ...config, robotId: 'other' }
            )
        await mount()
        await advance(6_000)
        expect(status()).toBe('Stream unavailable')
        expect(rooms).toHaveLength(0)
        expect(backendApi.getRobotMediaConfig).toHaveBeenCalledTimes(3)
        expect(vi.getTimerCount()).toBe(0)
    })

    test('shows localized camera status and retry action', async () => {
        window.localStorage.setItem('flotilla-language', 'no')
        backendApi.getRobotMediaConfig.mockResolvedValue(null)
        await mount()
        expect(status()).toBe('Kobler til på nytt')
        await advance(6_000)
        expect(status()).toBe('Kamerastrøm utilgjengelig')
        expect(container.querySelector('button')?.textContent).toBe('Prøv igjen')
    })
})
