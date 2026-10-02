import { NamedMediaStream } from 'models/VideoStream'
import { calculateMediaStreamRetryDelayMs, MEDIA_STREAM_RECOVERY_POLICY } from './MediaStreamRecoveryPolicy'

export interface OmeStreamState {
    cameraId: string
    attemptId: number
    status: 'connecting' | 'reconnecting' | 'connected' | 'unavailable'
    stream?: NamedMediaStream
}

type Timer = ReturnType<typeof setTimeout>

interface Camera {
    state: OmeStreamState
    attempts: number
    deadline?: Timer
    stableTimer?: Timer
    retryTimer?: Timer
}

export const createOmeStreamManager = (
    streams: NamedMediaStream[],
    refreshStream: (cameraId: string) => Promise<NamedMediaStream | undefined>,
    onChange: () => void
) => {
    let disposed = false
    const cameras = new Map<string, Camera>()
    const isCurrent = (camera: Camera, attemptId: number) => !disposed && camera.state.attemptId === attemptId

    const clearTimers = (camera: Camera) => {
        clearTimeout(camera.deadline)
        clearTimeout(camera.stableTimer)
        clearTimeout(camera.retryTimer)
        camera.deadline = camera.stableTimer = camera.retryTimer = undefined
    }

    const failCamera = (camera: Camera, attemptId: number) => {
        if (!isCurrent(camera, attemptId)) return
        clearTimers(camera)
        // Invalidate player callbacks and pending config responses before scheduling recovery.
        camera.state = {
            cameraId: camera.state.cameraId,
            attemptId: attemptId + 1,
            status: camera.attempts >= MEDIA_STREAM_RECOVERY_POLICY.maxAttempts ? 'unavailable' : 'reconnecting',
        }
        if (camera.state.status !== 'unavailable') {
            camera.retryTimer = setTimeout(
                () => void startCameraAttempt(camera),
                calculateMediaStreamRetryDelayMs(camera.attempts)
            )
        }
        onChange()
    }

    const startRecoveryDeadline = (camera: Camera) => {
        if (camera.deadline !== undefined) return
        const { attemptId } = camera.state
        camera.deadline = setTimeout(
            () => failCamera(camera, attemptId),
            MEDIA_STREAM_RECOVERY_POLICY.recoveryTimeoutMs
        )
    }

    const startCameraAttempt = async (camera: Camera) => {
        if (disposed) return
        clearTimers(camera)
        camera.attempts++
        camera.state = { ...camera.state, attemptId: camera.state.attemptId + 1, status: 'reconnecting' }
        const { attemptId, cameraId } = camera.state
        startRecoveryDeadline(camera)
        onChange()
        try {
            const stream = await refreshStream(cameraId)
            if (!isCurrent(camera, attemptId)) return
            if (!stream) {
                failCamera(camera, attemptId)
                return
            }
            camera.state = { ...camera.state, stream }
            onChange()
        } catch {
            failCamera(camera, attemptId)
        }
    }

    const markPlaying = (cameraId: string, attemptId: number) => {
        const camera = cameras.get(cameraId)
        if (!camera || !isCurrent(camera, attemptId) || !camera.state.stream) return
        if (camera.state.status === 'connected') return
        clearTimers(camera)
        camera.state = { ...camera.state, status: 'connected' }
        camera.stableTimer = setTimeout(() => {
            camera.attempts = 0
            camera.stableTimer = undefined
        }, MEDIA_STREAM_RECOVERY_POLICY.stabilityResetMs)
        onChange()
    }

    const markStalled = (cameraId: string, attemptId: number) => {
        const camera = cameras.get(cameraId)
        if (!camera || !isCurrent(camera, attemptId) || !camera.state.stream) return
        clearTimeout(camera.stableTimer)
        camera.stableTimer = undefined
        camera.state = { ...camera.state, status: 'reconnecting' }
        startRecoveryDeadline(camera)
        onChange()
    }

    const reconnect = (cameraId: string, attemptId: number) => {
        const camera = cameras.get(cameraId)
        if (camera) failCamera(camera, attemptId)
    }

    const retry = (cameraId: string) => {
        const camera = cameras.get(cameraId)
        if (!camera || camera.state.status !== 'unavailable' || disposed) return
        camera.attempts = 0
        void startCameraAttempt(camera)
    }

    streams.forEach((stream) => {
        const camera: Camera = {
            state: { cameraId: stream.cameraId, stream, attemptId: 1, status: 'connecting' },
            attempts: 1,
        }
        cameras.set(stream.cameraId, camera)
        startRecoveryDeadline(camera)
    })

    return {
        getStreams: () => [...cameras.values()].map((camera) => camera.state),
        markPlaying,
        markStalled,
        reconnect,
        retry,
        dispose: () => {
            disposed = true
            cameras.forEach(clearTimers)
        },
    }
}
