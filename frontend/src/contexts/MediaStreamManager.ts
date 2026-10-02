import { Room, RoomEvent } from 'livekit-client'
import { MediaConnectionType, MediaStreamConfig } from 'models/VideoStream'
import { createOmeStreamManager, OmeStreamState } from './OmeStreamManager'
import { calculateMediaStreamRetryDelayMs, MEDIA_STREAM_RECOVERY_POLICY } from './MediaStreamRecoveryPolicy'

export interface MediaStreamState {
    status: 'connecting' | 'reconnecting' | 'connected' | 'unavailable'
    streams: MediaStreamTrack[]
    omeStreams?: OmeStreamState[]
    omeAttemptId?: number
}

type TimerHandle = ReturnType<typeof setTimeout>

interface Attempt {
    id: number
    room?: Room
    timeout?: TimerHandle
    stableTimer?: TimerHandle
    tracks: Map<string, MediaStreamTrack>
    omeManager?: ReturnType<typeof createOmeStreamManager>
}

interface Connection {
    viewers: number
    attempts: number
    attempt?: Attempt
    retryTimer?: TimerHandle
    state: MediaStreamState
}

export const createMediaStreamManager = (
    getConfig: (robotId: string) => Promise<MediaStreamConfig | null | undefined>,
    onChange: (robotId: string, state: MediaStreamState | undefined) => void
) => {
    const connections = new Map<string, Connection>()
    let nextAttemptId = 0

    const disconnect = (room: Room) => {
        room.removeAllListeners()
        void room.disconnect().catch((error: unknown) => console.error('Failed to disconnect LiveKit room', error))
    }

    const clearAttempt = (connection: Connection) => {
        const attempt = connection.attempt
        connection.attempt = undefined
        if (!attempt) return
        clearTimeout(attempt.timeout)
        clearTimeout(attempt.stableTimer)
        attempt.omeManager?.dispose()
        if (attempt.room) disconnect(attempt.room)
    }

    const publish = (robotId: string, connection: Connection, status: MediaStreamState['status']) => {
        const attempt = connection.attempt
        connection.state = {
            status,
            streams: [...(attempt?.tracks.values() ?? [])],
            omeStreams: attempt?.omeManager?.getStreams(),
            omeAttemptId: attempt?.omeManager ? attempt.id : undefined,
        }
        onChange(robotId, connection.state)
    }

    const scheduleRetry = (robotId: string, connection: Connection) => {
        if (connection.attempts >= MEDIA_STREAM_RECOVERY_POLICY.maxAttempts) {
            publish(robotId, connection, 'unavailable')
            return
        }
        publish(robotId, connection, 'reconnecting')
        connection.retryTimer = setTimeout(() => {
            connection.retryTimer = undefined
            void startAttempt(robotId, connection)
        }, calculateMediaStreamRetryDelayMs(connection.attempts))
    }

    const createAttemptHandlers = (robotId: string, connection: Connection, attempt: Attempt) => {
        const isCurrent = () => connections.get(robotId) === connection && connection.attempt === attempt

        const fail = (reason: unknown) => {
            if (!isCurrent()) return
            console.warn(`Livestream failed for robot ${robotId}`, reason)
            clearAttempt(connection)
            scheduleRetry(robotId, connection)
        }

        // Preserve an existing deadline so repeated events cannot postpone recovery indefinitely.
        const startRecoveryDeadlineIfNeeded = () => {
            if (attempt.timeout !== undefined) return
            attempt.timeout = setTimeout(
                () => fail('Timed out waiting for camera stream'),
                MEDIA_STREAM_RECOVERY_POLICY.recoveryTimeoutMs
            )
        }
        const clearRecoveryDeadline = () => {
            clearTimeout(attempt.timeout)
            attempt.timeout = undefined
        }
        const clearStability = () => {
            clearTimeout(attempt.stableTimer)
            attempt.stableTimer = undefined
        }
        const markConnected = () => {
            clearRecoveryDeadline()
            publish(robotId, connection, 'connected')
            if (attempt.stableTimer !== undefined || connection.attempts === 0) return
            // Replenish recovery for a later outage, but not for rapidly flapping publishers.
            attempt.stableTimer = setTimeout(() => {
                if (isCurrent()) connection.attempts = 0
                attempt.stableTimer = undefined
            }, MEDIA_STREAM_RECOVERY_POLICY.stabilityResetMs)
        }
        const registerRoomEvents = (room: Room) => {
            const removeTrack = (trackSid: string) => {
                if (!isCurrent() || !attempt.tracks.delete(trackSid)) return
                if (attempt.tracks.size === 0) {
                    clearStability()
                    publish(robotId, connection, 'reconnecting')
                    startRecoveryDeadlineIfNeeded()
                } else {
                    publish(robotId, connection, connection.state.status)
                }
            }
            room.on(RoomEvent.TrackSubscribed, (track, publication) => {
                if (!isCurrent() || track.kind !== 'video') return
                attempt.tracks.set(publication.trackSid, track.mediaStreamTrack)
                markConnected()
            })
            room.on(RoomEvent.TrackUnsubscribed, (_track, publication) => removeTrack(publication.trackSid))
            room.on(RoomEvent.TrackUnpublished, (publication) => removeTrack(publication.trackSid))
            room.on(RoomEvent.Disconnected, () => fail('LiveKit room disconnected'))
            const reconnecting = () => {
                if (!isCurrent()) return
                clearStability()
                publish(robotId, connection, 'reconnecting')
                startRecoveryDeadlineIfNeeded()
            }
            room.on(RoomEvent.Reconnecting, reconnecting)
            room.on(RoomEvent.SignalReconnecting, reconnecting)
            room.on(RoomEvent.Reconnected, () => {
                if (!isCurrent()) return
                if (attempt.tracks.size > 0) {
                    markConnected()
                }
            })
        }

        return { isCurrent, fail, startRecoveryDeadlineIfNeeded, registerRoomEvents }
    }

    const startAttempt = async (robotId: string, connection: Connection) => {
        if (connections.get(robotId) !== connection) return
        const attempt: Attempt = { id: ++nextAttemptId, tracks: new Map() }
        connection.attempt = attempt
        connection.attempts++
        const { isCurrent, fail, startRecoveryDeadlineIfNeeded, registerRoomEvents } = createAttemptHandlers(
            robotId,
            connection,
            attempt
        )
        publish(robotId, connection, connection.state.status === 'connecting' ? 'connecting' : 'reconnecting')
        startRecoveryDeadlineIfNeeded()

        try {
            // This endpoint activates the robot's publisher; a valid cached token cannot replace it.
            const config = await getConfig(robotId)
            if (!isCurrent()) return
            if (!config || config.robotId !== robotId) {
                fail('No supported media configuration')
                return
            }
            if (config.mediaConnectionType === MediaConnectionType.OvenMediaEngine && config.streams?.length) {
                clearTimeout(attempt.timeout)
                attempt.timeout = undefined
                const refreshOmeStreamForCamera = async (cameraId: string) => {
                    const refreshed = await getConfig(robotId)
                    if (
                        refreshed?.robotId !== robotId ||
                        refreshed.mediaConnectionType !== MediaConnectionType.OvenMediaEngine
                    )
                        return undefined
                    return refreshed.streams?.find((stream) => stream.cameraId === cameraId)
                }
                const publishOmeCameraStatus = () => {
                    if (!isCurrent()) return
                    const cameras = attempt.omeManager?.getStreams() ?? []
                    for (const status of ['reconnecting', 'connecting', 'connected', 'unavailable'] as const) {
                        if (cameras.some((camera) => camera.status === status)) {
                            publish(robotId, connection, status)
                            return
                        }
                    }
                }
                attempt.omeManager = createOmeStreamManager(
                    config.streams,
                    refreshOmeStreamForCamera,
                    publishOmeCameraStatus
                )
                publish(robotId, connection, connection.state.status)
                return
            }
            if (config.mediaConnectionType !== MediaConnectionType.LiveKit) {
                fail('No supported media configuration')
                return
            }
            const room = new Room()
            attempt.room = room
            registerRoomEvents(room)
            await room.connect(config.url, config.token)
            // connect() may settle after timeout, release, or provider cleanup.
            if (!isCurrent()) disconnect(room)
        } catch (error) {
            fail(error)
        }
    }

    const removeConnection = (robotId: string, connection: Connection) => {
        connections.delete(robotId)
        clearTimeout(connection.retryTimer)
        clearAttempt(connection)
        onChange(robotId, undefined)
    }

    const acquire = (robotId: string) => {
        let connection = connections.get(robotId)
        if (!connection) {
            connection = { viewers: 0, attempts: 0, state: { status: 'connecting', streams: [] } }
            connections.set(robotId, connection)
            const newConnection = connection
            // StrictMode's setup/cleanup replay can cancel before making an activation request.
            void Promise.resolve().then(() => startAttempt(robotId, newConnection))
        }
        connection.viewers++
        const ownedConnection = connection
        let released = false
        return () => {
            if (released || connections.get(robotId) !== ownedConnection) return
            released = true
            ownedConnection.viewers--
            if (ownedConnection.viewers === 0) removeConnection(robotId, ownedConnection)
        }
    }

    const retry = (robotId: string) => {
        const connection = connections.get(robotId)
        if (!connection || connection.state.status !== 'unavailable') return
        clearAttempt(connection)
        connection.attempts = 0
        connection.state = { status: 'connecting', streams: [] }
        void startAttempt(robotId, connection)
    }

    const currentOmeManager = (robotId: string, attemptId: number) => {
        const attempt = connections.get(robotId)?.attempt
        if (attempt?.id === attemptId) return attempt.omeManager
    }

    const markOmeStreamPlaying = (robotId: string, attemptId: number, cameraId: string, cameraAttemptId: number) => {
        currentOmeManager(robotId, attemptId)?.markPlaying(cameraId, cameraAttemptId)
    }

    const markOmeStreamStalled = (robotId: string, attemptId: number, cameraId: string, cameraAttemptId: number) => {
        currentOmeManager(robotId, attemptId)?.markStalled(cameraId, cameraAttemptId)
    }

    const reconnectOmeStream = (robotId: string, attemptId: number, cameraId: string, cameraAttemptId: number) => {
        currentOmeManager(robotId, attemptId)?.reconnect(cameraId, cameraAttemptId)
    }

    const retryOmeStream = (robotId: string, attemptId: number, cameraId: string) => {
        currentOmeManager(robotId, attemptId)?.retry(cameraId)
    }

    const dispose = () => {
        connections.forEach((connection, robotId) => removeConnection(robotId, connection))
    }

    return { acquire, retry, markOmeStreamPlaying, markOmeStreamStalled, reconnectOmeStream, retryOmeStream, dispose }
}
