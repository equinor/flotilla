import { Ref, useImperativeHandle, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Icon, Menu, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useBackendApi } from 'api/UseBackendApi'
import { useLanguageContext } from 'contexts/LanguageContext'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { ApiError } from 'api/ApiError'
import { CameraRecording, MissionRecordingAccess, RecordingSeekRequest } from 'models/MissionRecording'
import { useRecordingPlayback } from './useRecordingPlayback'
import { getRecordingTimestamp } from './recordingTime'

// Renew playback access one minute before expiry to allow time for the request.
const PLAYBACK_ACCESS_RENEWAL_MARGIN_MS = 60_000

// Wait at least ten seconds between renewal attempts to avoid rapid retries.
const MINIMUM_RETRY_INTERVAL_MS = 10_000

const Player = styled.div`
    width: 100%;
    min-width: 0;
`

const PlayerViewport = styled.div<{ $aspectRatio: number }>`
    position: relative;
    width: 100%;
    aspect-ratio: ${({ $aspectRatio }) => $aspectRatio};
    overflow: hidden;
    border-radius: 0.5rem;
    background: #000;

    video {
        display: block;
        width: 100%;
        height: 100%;
        object-fit: contain;
    }
`

const UnavailableOverlay = styled.div`
    position: absolute;
    inset: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    padding: 1rem;
    text-align: center;
    background: ${tokens.colors.ui.background__light.hex};
`

const PlayerToolbar = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    padding: 0 0.25rem 0.5rem;

    > :first-child {
        min-width: 0;
    }
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

export interface RecordingPlayerControls {
    play: () => void
    pause: () => void
    isPlaying: () => boolean
    getPlaybackProgress: () => { timestamp: number; playing: boolean } | undefined
}

interface RecordingPlayerProps {
    ref?: Ref<RecordingPlayerControls>
    missionId: string
    camera: CameraRecording
    seekRequest: RecordingSeekRequest | undefined
    sharedPlaybackTimestamp: number | undefined
    onPlaybackProgress: () => void
    onAspectRatioChange: (aspectRatio: number) => void
}

export const RecordingPlayer = ({
    ref,
    missionId,
    camera,
    seekRequest,
    sharedPlaybackTimestamp,
    onPlaybackProgress,
    onAspectRatioChange,
}: RecordingPlayerProps) => {
    const backendApi = useBackendApi()
    const { TranslateText } = useLanguageContext()
    const [isMenuOpen, setIsMenuOpen] = useState(false)
    const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null)
    const [videoAspectRatio, setVideoAspectRatio] = useState(16 / 9)
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
    const { videoRef, seekMessage, playbackFailed, reportPlaybackError, pausePlayback, missingStartTime } =
        useRecordingPlayback(playbackAccess?.playbackUrl, camera.firstFrameAt, seekRequest, sharedPlaybackTimestamp)
    const playbackUnavailable = isError || playbackFailed
    const unavailableMessage = playbackUnavailable ? 'Recording unavailable.' : seekMessage
    const recordingStart = getRecordingTimestamp(camera.firstFrameAt)

    useImperativeHandle(
        ref,
        () => ({
            play: () => {
                const video = videoRef.current
                if (!video || unavailableMessage || video.error || video.ended) return
                if (video.readyState < HTMLMediaElement.HAVE_METADATA) return
                void video.play().catch(() => undefined)
            },
            pause: pausePlayback,
            isPlaying: () => {
                const video = videoRef.current
                return video !== null && !video.paused && !video.ended
            },
            getPlaybackProgress: () => {
                const video = videoRef.current
                if (!video || unavailableMessage || video.error || video.seeking || recordingStart === undefined) return
                if (video.readyState < HTMLMediaElement.HAVE_METADATA) return
                return {
                    timestamp: recordingStart + video.currentTime * 1000,
                    playing: !video.paused && !video.ended,
                }
            },
        }),
        [unavailableMessage, videoRef, pausePlayback, recordingStart]
    )

    return (
        <Player>
            <PlayerToolbar>
                <Typography variant="h5" lines={1}>
                    {camera.cameraId}
                </Typography>
                {playbackAccess && (
                    <>
                        <Button
                            variant="ghost_icon"
                            ref={setAnchorEl}
                            aria-haspopup="menu"
                            aria-expanded={isMenuOpen}
                            onClick={() => setIsMenuOpen(!isMenuOpen)}
                        >
                            <Icon name={Icons.Menu} />
                        </Button>
                        <Menu open={isMenuOpen} anchorEl={anchorEl} onClose={() => setIsMenuOpen(false)}>
                            <Menu.Item as="a" href={playbackAccess.downloadUrl} onClick={() => setIsMenuOpen(false)}>
                                Download video
                            </Menu.Item>
                        </Menu>
                    </>
                )}
            </PlayerToolbar>
            {missingStartTime && (
                <Typography>Recording start time unavailable; use individual video controls.</Typography>
            )}
            <PlayerViewport $aspectRatio={videoAspectRatio}>
                <video
                    ref={videoRef}
                    controls
                    preload="metadata"
                    style={{ visibility: unavailableMessage ? 'hidden' : undefined }}
                    onError={reportPlaybackError}
                    onTimeUpdate={onPlaybackProgress}
                    onPlay={onPlaybackProgress}
                    onPause={onPlaybackProgress}
                    onEnded={onPlaybackProgress}
                    onSeeked={onPlaybackProgress}
                    onLoadedMetadata={(event) => {
                        const { videoWidth, videoHeight } = event.currentTarget
                        if (videoWidth > 0 && videoHeight > 0) {
                            const aspectRatio = videoWidth / videoHeight
                            setVideoAspectRatio(aspectRatio)
                            onAspectRatioChange(aspectRatio)
                        }
                    }}
                />
                {unavailableMessage && (
                    <UnavailableOverlay role="status">
                        <Typography color={tokens.colors.text.static_icons__default.hex}>
                            {TranslateText(unavailableMessage)}
                        </Typography>
                        {playbackUnavailable && (
                            <Button onClick={() => void refetch().then(() => videoRef.current?.load())}>Retry</Button>
                        )}
                    </UnavailableOverlay>
                )}
            </PlayerViewport>
        </Player>
    )
}
