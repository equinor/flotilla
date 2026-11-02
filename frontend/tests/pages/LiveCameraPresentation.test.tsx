import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import { OmeVideoCard } from 'pages/MissionPage/VideoStream/OmeVideoCard'
import { VideoStreamCard } from 'pages/MissionPage/VideoStream/VideoStreamCards'

let root: Root
let container: HTMLDivElement

beforeEach(() => {
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    Object.defineProperty(document, 'fullscreenElement', { configurable: true, get: () => null })
    vi.stubGlobal(
        'WebSocket',
        class {
            close() {}
        }
    )
    vi.stubGlobal(
        'ResizeObserver',
        class {
            observe() {}
            disconnect() {}
        }
    )
    container = document.createElement('div')
    document.body.append(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    container.remove()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    Reflect.deleteProperty(document, 'fullscreenElement')
    Reflect.deleteProperty(document, 'exitFullscreen')
})

const setVideoDimensions = (video: HTMLVideoElement, width: number, height: number, event = 'loadedmetadata') => {
    Object.defineProperties(video, {
        videoWidth: { configurable: true, value: width },
        videoHeight: { configurable: true, value: height },
    })
    act(() => video.dispatchEvent(new Event(event)))
}

const getAspectRatio = (element: Element) => {
    const [width, height = 1] = getComputedStyle(element).aspectRatio.split('/').map(Number)
    return width / height
}

const mountOmeCamera = (rotationDegrees = 0) => {
    const onAspectRatioChange = vi.fn()
    act(() =>
        root.render(
            <OmeVideoCard
                stream={{ cameraId: 'front', url: 'wss://camera.example/front', rotationDegrees }}
                onAspectRatioChange={onAspectRatioChange}
                onPlaying={vi.fn()}
                onStalled={vi.fn()}
                onDisconnect={vi.fn()}
            />
        )
    )
    const frame = container.firstElementChild as HTMLDivElement
    const video = document.createElement('video')
    frame.firstElementChild!.append(video)
    return { frame, video, onAspectRatioChange }
}

describe('compact live cameras', () => {
    test.each([
        [0, 4 / 3],
        [90, 3 / 4],
        [180, 4 / 3],
        [270, 3 / 4],
        [-90, 3 / 4],
    ])('fits the actual footage at %s degrees without a fixed widescreen frame', (rotation, ratio) => {
        const { frame, video, onAspectRatioChange } = mountOmeCamera(rotation)
        setVideoDimensions(video, 640, 480)

        expect(getAspectRatio(frame)).toBeCloseTo(ratio)
        expect(onAspectRatioChange).toHaveBeenLastCalledWith(ratio)
    })

    test('updates dimensions when a stream changes resolution and ignores empty metadata', () => {
        const { frame, video, onAspectRatioChange } = mountOmeCamera(90)
        setVideoDimensions(video, 640, 480)
        setVideoDimensions(video, 1920, 1080, 'resize')
        expect(getAspectRatio(frame)).toBeCloseTo(9 / 16)
        expect(onAspectRatioChange).toHaveBeenLastCalledWith(9 / 16)

        setVideoDimensions(video, 0, 0, 'resize')
        expect(getAspectRatio(frame)).toBeCloseTo(9 / 16)
    })

    test('keeps fullscreen available through both the button and double-click', () => {
        const { frame } = mountOmeCamera(90)
        const requestFullscreen = vi.fn().mockResolvedValue(undefined)
        frame.requestFullscreen = requestFullscreen
        const button = frame.querySelector('button')!
        expect(button.getAttribute('aria-label')).toBe('Enter fullscreen')

        act(() => button.click())
        act(() => frame.dispatchEvent(new MouseEvent('dblclick', { bubbles: true })))
        expect(requestFullscreen).toHaveBeenCalledTimes(2)

        const exitFullscreen = vi.fn().mockResolvedValue(undefined)
        document.exitFullscreen = exitFullscreen
        vi.spyOn(document, 'fullscreenElement', 'get').mockReturnValue(frame)
        act(() => document.dispatchEvent(new Event('fullscreenchange')))
        expect(button.getAttribute('aria-label')).toBe('Exit fullscreen')
        act(() => button.click())
        expect(exitFullscreen).toHaveBeenCalledOnce()
    })

    test('also fits LiveKit video tracks to their natural aspect ratio', () => {
        const onAspectRatioChange = vi.fn()
        const stream = {} as MediaStream
        act(() =>
            root.render(
                <VideoStreamCard
                    videoStream={stream}
                    videoStreamId="livekit-front"
                    onAspectRatioChange={onAspectRatioChange}
                />
            )
        )
        const video = container.querySelector('video')!
        setVideoDimensions(video, 1280, 1024)

        expect(video.srcObject).toBe(stream)
        expect(getAspectRatio(video.parentElement!)).toBeCloseTo(1.25)
        expect(onAspectRatioChange).toHaveBeenLastCalledWith(1.25)
        expect(container.querySelector('button')?.getAttribute('aria-label')).toBe('Enter fullscreen')
    })
})
