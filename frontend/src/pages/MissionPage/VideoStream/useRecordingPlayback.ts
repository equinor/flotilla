import { useEffect, useRef, useState } from 'react'
import { RecordingSeekRequest } from 'models/MissionRecording'
import { getRecordingTimestamp } from './recordingTime'

export const useRecordingPlayback = (
    playbackUrl: string | undefined,
    firstFrameAt: string | undefined,
    seekRequest: RecordingSeekRequest | undefined,
    sharedPlaybackTimestamp: number | undefined
) => {
    const videoRef = useRef<HTMLVideoElement>(null)
    const appliedSeek = useRef<RecordingSeekRequest | undefined>(undefined)
    const resumeAfterLoad = useRef(false)
    const [seekMessage, setSeekMessage] = useState<string>()
    const [playbackFailed, setPlaybackFailed] = useState(false)
    const recordingStart = getRecordingTimestamp(firstFrameAt)

    useEffect(() => {
        const video = videoRef.current
        if (!video || !playbackUrl) return

        const sourceChanged = video.getAttribute('src') !== playbackUrl
        const previousPosition = video.currentTime
        if (sourceChanged) resumeAfterLoad.current = !video.paused && !video.ended

        const applyPlaybackPosition = () => {
            const hasPendingSeek = seekRequest && seekRequest !== appliedSeek.current
            const timestamp = sharedPlaybackTimestamp ?? (hasPendingSeek ? seekRequest.timestamp : undefined)
            if (timestamp !== undefined && recordingStart !== undefined) {
                const position = (timestamp - recordingStart) / 1000
                let unavailableReason: string | undefined
                if (position < 0) unavailableReason = 'Recording starts after the selected time.'
                else if (!Number.isFinite(video.duration)) unavailableReason = 'Recording duration unavailable.'
                else if (position >= video.duration) unavailableReason = 'Recording ended before the selected time.'

                setSeekMessage(unavailableReason)
                appliedSeek.current = seekRequest
                if (unavailableReason) {
                    video.pause()
                    return
                }
                if (sharedPlaybackTimestamp === undefined || hasPendingSeek || video.paused || sourceChanged) {
                    video.currentTime = position
                }
            } else if (sourceChanged) {
                video.currentTime = previousPosition
            }
            if (
                video.paused &&
                !video.error &&
                (sharedPlaybackTimestamp !== undefined || (sourceChanged && resumeAfterLoad.current))
            ) {
                void video.play().catch(() => undefined)
            }
        }

        const handleLoadedMetadata = () => {
            setPlaybackFailed(false)
            applyPlaybackPosition()
        }

        video.addEventListener('loadedmetadata', handleLoadedMetadata)
        if (sourceChanged) video.src = playbackUrl
        else if (video.readyState >= HTMLMediaElement.HAVE_METADATA) applyPlaybackPosition()

        return () => video.removeEventListener('loadedmetadata', handleLoadedMetadata)
    }, [playbackUrl, recordingStart, seekRequest, sharedPlaybackTimestamp])

    return {
        videoRef,
        seekMessage,
        playbackFailed,
        reportPlaybackError: () => setPlaybackFailed(true),
        pausePlayback: () => {
            resumeAfterLoad.current = false
            videoRef.current?.pause()
        },
        missingStartTime: recordingStart === undefined,
    }
}
