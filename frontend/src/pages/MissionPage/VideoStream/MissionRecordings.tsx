import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Icon, Menu, Typography } from '@equinor/eds-core-react'
import { useBackendApi } from 'api/UseBackendApi'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { ApiError } from 'api/ApiError'

// Renew playback access one minute before expiry to allow time for the request.
const PLAYBACK_ACCESS_RENEWAL_MARGIN_MS = 60_000

// Wait at least ten seconds between renewal attempts to avoid rapid retries.
const MINIMUM_RETRY_INTERVAL_MS = 10_000

const Cameras = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 1rem;
`

const Camera = styled.div`
    min-width: 0;
    max-width: 100%;
`

const Player = styled.div`
    width: fit-content;
    max-width: 100%;
    video {
        display: block;
        height: min(18rem, 50vw);
        width: auto;
        max-width: 100%;
    }
`

const PlayerToolbar = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
`

const shouldRenewPlaybackAccess = (playbackAccess: MissionRecordingAccess | undefined) => {
    if (!playbackAccess) return false

    const expiresInMs = Date.parse(playbackAccess.expiresAt) - Date.now()

    return expiresInMs <= PLAYBACK_ACCESS_RENEWAL_MARGIN_MS
}

const canRefreshPlaybackAccess = (
    playbackAccess: MissionRecordingAccess | undefined,
    error: Error | null
): playbackAccess is MissionRecordingAccess => {
    if (!playbackAccess) return false
    if (error instanceof ApiError && error.statusCode === 403) return false

    return true
}

const getPlaybackAccessRefreshInterval = (playbackAccess: MissionRecordingAccess) => {
    const expiresInMs = Date.parse(playbackAccess.expiresAt) - Date.now()

    return Math.max(expiresInMs - PLAYBACK_ACCESS_RENEWAL_MARGIN_MS, MINIMUM_RETRY_INTERVAL_MS)
}

const RecordingPlayer = ({ missionId, camera }: { missionId: string; camera: MissionRecording['cameras'][string] }) => {
    const backendApi = useBackendApi()
    const [isMenuOpen, setIsMenuOpen] = useState(false)
    const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null)
    const videoRef = useRef<HTMLVideoElement>(null)
    const {
        data: playbackAccess,
        isError,
        refetch,
    } = useQuery({
        queryKey: ['missionRecordingAccess', missionId, camera.cameraId, camera.key],
        queryFn: () => backendApi.getMissionRecordingAccess(missionId, camera.key),
        retry: 1,
        gcTime: 0,
        refetchOnWindowFocus: ({ state }) => shouldRenewPlaybackAccess(state.data),
        refetchInterval: ({ state: { data, error } }) => {
            if (!canRefreshPlaybackAccess(data, error)) return false
            return getPlaybackAccessRefreshInterval(data)
        },
        refetchIntervalInBackground: true,
    })
    const playbackUrl = playbackAccess?.playbackUrl

    useEffect(() => {
        const video = videoRef.current
        if (!video || !playbackUrl) return
        const position = video.currentTime
        const wasPlaying = !video.paused && !video.ended
        const restorePlayback = () => {
            video.currentTime = position
            if (wasPlaying) void video.play().catch(() => undefined)
        }
        video.addEventListener('loadedmetadata', restorePlayback, { once: true })
        video.src = playbackUrl
        return () => video.removeEventListener('loadedmetadata', restorePlayback)
    }, [playbackUrl])

    return (
        <Player>
            <PlayerToolbar>
                <Typography variant="h5">{camera.cameraId}</Typography>
                {playbackAccess && (
                    <>
                        <Button
                            variant="ghost_icon"
                            ref={setAnchorEl}
                            aria-label={`Video options for ${camera.cameraId}`}
                            aria-haspopup="menu"
                            aria-expanded={isMenuOpen}
                            onClick={() => setIsMenuOpen(!isMenuOpen)}
                        >
                            <Icon name={Icons.Menu} />
                        </Button>
                        <Menu
                            open={isMenuOpen}
                            anchorEl={anchorEl}
                            aria-label={`Video options for ${camera.cameraId}`}
                            onClose={() => setIsMenuOpen(false)}
                        >
                            <Menu.Item as="a" href={playbackAccess.downloadUrl} onClick={() => setIsMenuOpen(false)}>
                                Download video
                            </Menu.Item>
                        </Menu>
                    </>
                )}
            </PlayerToolbar>
            {isError && (
                <Typography role="status">
                    Recording unavailable. <Button onClick={() => void refetch()}>Retry</Button>
                </Typography>
            )}
            <video ref={videoRef} controls preload="metadata" />
        </Player>
    )
}

export interface MissionRecording {
    status: string
    engine?: string
    gaps: { at: string; reason: string }[]
    cameras: Record<
        string,
        {
            cameraId: string
            key: string
            firstFrameAt?: string
            status: string
            error?: string
        }
    >
}

export interface MissionRecordingAccess {
    playbackUrl: string
    downloadUrl: string
    expiresAt: string
}

export const MissionRecordings = ({ missionId }: { missionId: string }) => {
    const backendApi = useBackendApi()
    const [recording, setRecording] = useState<MissionRecording>()
    const [error, setError] = useState<'unavailable' | 'forbidden'>()
    useEffect(() => {
        let disposed = false
        let timer: ReturnType<typeof setTimeout>
        const refresh = () =>
            backendApi
                .getMissionRecordings(missionId)
                .then((result) => {
                    if (!disposed) {
                        setRecording(result)
                        setError(undefined)
                        timer = setTimeout(() => void refresh(), result.status === 'Ready' ? 600000 : 2000)
                    }
                })
                .catch((requestError: unknown) => {
                    if (!disposed) {
                        const forbidden = requestError instanceof ApiError && requestError.statusCode === 403
                        setError(forbidden ? 'forbidden' : 'unavailable')
                        if (!forbidden) timer = setTimeout(() => void refresh(), 10000)
                    }
                })
        void refresh()
        return () => {
            disposed = true
            clearTimeout(timer)
        }
    }, [backendApi, missionId])

    if (error === 'forbidden' || (!recording && !error)) return null

    return (
        <section>
            <Typography variant="h2">Mission camera recordings</Typography>
            {(error || recording?.status !== 'Ready') && (
                <Typography role="status">
                    {error ? 'Recordings unavailable.' : (recording?.status ?? 'Loading recordings…')}
                </Typography>
            )}
            {!!recording?.gaps.length && (
                <Typography>Recording interruptions detected; some footage may be missing.</Typography>
            )}
            <Cameras>
                {Object.values(recording?.cameras ?? {}).map((camera) => (
                    <Camera key={camera.key}>
                        {camera.status !== 'Ready' && <Typography variant="h5">{camera.cameraId}</Typography>}
                        {(camera.status !== 'Ready' || camera.error) && (
                            <Typography>
                                {camera.status}
                                {camera.error ? `: ${camera.error}` : ''}
                            </Typography>
                        )}
                        {camera.status === 'Ready' && <RecordingPlayer missionId={missionId} camera={camera} />}
                    </Camera>
                ))}
            </Cameras>
        </section>
    )
}
