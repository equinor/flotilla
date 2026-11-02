import { Button, Typography } from '@equinor/eds-core-react'
import { VideoStreamCard } from './VideoStreamCards'
import styled from 'styled-components'
import { useMediaStreamContext } from 'contexts/MediaStreamContext'
import { useEffect, useMemo, useState } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { OmeVideoCard } from './OmeVideoCard'
import { LiveCamera, LiveCameraRow, LiveCameraStage, LiveCameraStatus, LiveCameraViewport } from './LiveCameraLayout'
import { useCameraReveal } from './useCameraReveal'

const RetryButton = styled(Button)`
    width: fit-content;
`

const statusLabels = {
    connecting: 'Connecting',
    reconnecting: 'Reconnecting',
    unavailable: 'Stream unavailable',
    connected: '',
}

interface VideoStreamWindowProps {
    robotId: string
}

export const VideoStreamWindow = ({ robotId }: VideoStreamWindowProps) => {
    const { TranslateText } = useLanguageContext()
    const [aspectRatios, setAspectRatios] = useState<Record<string, number>>({})
    const updateAspectRatio = (cameraKey: string, aspectRatio: number) => {
        setAspectRatios((ratios) =>
            ratios[cameraKey] === aspectRatio ? ratios : { ...ratios, [cameraKey]: aspectRatio }
        )
    }
    const {
        mediaStreams,
        acquireMediaStream,
        retryMediaStream,
        markOmeStreamPlaying,
        markOmeStreamStalled,
        reconnectOmeStream,
        retryOmeStream,
    } = useMediaStreamContext()
    useEffect(() => acquireMediaStream(robotId), [robotId, acquireMediaStream])

    const {
        streams: videoStreams,
        omeStreams,
        omeAttemptId,
        status,
    } = mediaStreams[robotId] ?? {
        streams: [],
        status: 'connecting',
    }
    const statusText = statusLabels[status]
    const liveKitStreams = useMemo(
        () => videoStreams.map((track) => ({ id: track.id, stream: new MediaStream([track]) })),
        [videoStreams]
    )
    const cameraKeys = [
        ...liveKitStreams.map(({ id }) => `${robotId}-${id}`),
        ...(omeStreams ?? []).map(({ cameraId }) => `${robotId}-${cameraId}`),
    ]
    const isRevealed = useCameraReveal(robotId, cameraKeys, aspectRatios)
    const videoCards = liveKitStreams.map(({ id, stream }) => {
        const cameraKey = `${robotId}-${id}`
        return (
            <LiveCamera key={cameraKey} $aspectRatio={aspectRatios[cameraKey] ?? 16 / 9}>
                <VideoStreamCard
                    videoStream={stream}
                    videoStreamName={undefined}
                    videoStreamId={cameraKey}
                    onAspectRatioChange={(ratio) => updateAspectRatio(cameraKey, ratio)}
                />
            </LiveCamera>
        )
    })

    return (
        <>
            <Typography variant="h2">{TranslateText('Camera')}</Typography>
            {!omeStreams && statusText && <Typography role="status">{TranslateText(statusText)}</Typography>}
            {!omeStreams && status === 'unavailable' && (
                <RetryButton onClick={() => retryMediaStream(robotId)}>{TranslateText('Retry')}</RetryButton>
            )}
            <LiveCameraStage>
                {!isRevealed && cameraKeys.length > 0 && (
                    <Typography aria-live="polite">{TranslateText('Connecting cameras…')}</Typography>
                )}
                <LiveCameraRow $visible={isRevealed} aria-hidden={!isRevealed} inert={!isRevealed}>
                    {videoCards}
                    {omeAttemptId !== undefined &&
                        omeStreams?.map((camera) => {
                            const cameraKey = `${robotId}-${camera.cameraId}`
                            const quarterTurn = Math.abs((camera.stream?.rotationDegrees ?? 0) % 180) === 90
                            const aspectRatio = aspectRatios[cameraKey] ?? (quarterTurn ? 9 / 16 : 16 / 9)
                            return (
                                <LiveCamera key={cameraKey} $aspectRatio={aspectRatio}>
                                    <Typography variant="h5" lines={1}>
                                        {camera.cameraId}
                                    </Typography>
                                    <LiveCameraViewport $aspectRatio={aspectRatio}>
                                        {camera.stream && (
                                            <OmeVideoCard
                                                key={camera.attemptId}
                                                stream={camera.stream}
                                                aspectRatio={aspectRatio}
                                                onAspectRatioChange={(ratio) => updateAspectRatio(cameraKey, ratio)}
                                                onPlaying={() =>
                                                    markOmeStreamPlaying(
                                                        robotId,
                                                        omeAttemptId,
                                                        camera.cameraId,
                                                        camera.attemptId
                                                    )
                                                }
                                                onStalled={() =>
                                                    markOmeStreamStalled(
                                                        robotId,
                                                        omeAttemptId,
                                                        camera.cameraId,
                                                        camera.attemptId
                                                    )
                                                }
                                                onDisconnect={() =>
                                                    reconnectOmeStream(
                                                        robotId,
                                                        omeAttemptId,
                                                        camera.cameraId,
                                                        camera.attemptId
                                                    )
                                                }
                                            />
                                        )}
                                        {camera.status !== 'connected' && (
                                            <LiveCameraStatus>
                                                <Typography role="status" color="#fff">
                                                    {TranslateText(statusLabels[camera.status])}
                                                </Typography>
                                                {camera.status === 'unavailable' && (
                                                    <Button
                                                        onClick={() =>
                                                            retryOmeStream(robotId, omeAttemptId, camera.cameraId)
                                                        }
                                                    >
                                                        {TranslateText('Retry')}
                                                    </Button>
                                                )}
                                            </LiveCameraStatus>
                                        )}
                                    </LiveCameraViewport>
                                </LiveCamera>
                            )
                        })}
                </LiveCameraRow>
            </LiveCameraStage>
        </>
    )
}
