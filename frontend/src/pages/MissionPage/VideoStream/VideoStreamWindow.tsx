import { Button, CircularProgress, Typography } from '@equinor/eds-core-react'
import { VideoStreamCard } from './VideoStreamCards'
import styled from 'styled-components'
import { useMediaStreamContext } from 'contexts/MediaStreamContext'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { CameraStatusOverlay, LiveCamera, LiveCameraRow, LiveCameraStage } from './LiveCameraLayout'
import { useCameraReveal } from './useCameraReveal'
import { CameraPlaceholders } from './CameraPlaceholders'
import { useCameraPositions } from './useCameraPositions'
import { OmeCameraTile } from './OmeCameraTile'
import { mediaStreamStatusLabels } from './mediaStreamStatusLabels'

const RetryButton = styled(Button)`
    width: fit-content;
`

const ScreenReaderStatus = styled.span.attrs({ 'aria-live': 'polite' })`
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
    border: 0;
`

interface VideoStreamWindowProps {
    robotId: string
}

export const VideoStreamWindow = ({ robotId }: VideoStreamWindowProps) => {
    const { TranslateText } = useLanguageContext()
    const stage = useRef<HTMLDivElement>(null)
    const [aspectRatios, setAspectRatios] = useState<Record<string, number>>({})
    const updateAspectRatio = (cameraKey: string, aspectRatio: number) => {
        setAspectRatios((ratios) =>
            ratios[cameraKey] === aspectRatio ? ratios : { ...ratios, [cameraKey]: aspectRatio }
        )
    }
    const { mediaStreams, acquireMediaStream, retryMediaStream } = useMediaStreamContext()
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
    const statusText = mediaStreamStatusLabels[status]
    const liveKitStreams = useMemo(
        () => videoStreams.map((track) => ({ id: track.id, stream: new MediaStream([track]) })),
        [videoStreams]
    )
    const cameras = [
        ...liveKitStreams.map(({ id }) => {
            const key = `${robotId}-${id}`
            return { key, aspectRatio: aspectRatios[key] ?? 16 / 9 }
        }),
        ...(omeStreams ?? []).map((camera) => {
            const key = `${robotId}-${camera.cameraId}`
            const quarterTurn = Math.abs((camera.stream?.rotationDegrees ?? 0) % 180) === 90
            return { key, aspectRatio: aspectRatios[key] ?? (quarterTurn ? 9 / 16 : 16 / 9) }
        }),
    ]
    const cameraKeys = cameras.map(({ key }) => key)
    const isRevealed = useCameraReveal(robotId, cameraKeys, aspectRatios)
    const positions = useCameraPositions(stage, cameras)
    const showCameras = isRevealed && cameraKeys.every((key) => positions[key] !== undefined)
    const placeholderCount = omeStreams?.length ?? (liveKitStreams.length || 5)
    const videoCards = liveKitStreams.map(({ id, stream }) => {
        const cameraKey = `${robotId}-${id}`
        return (
            <LiveCamera key={cameraKey} $position={positions[cameraKey]}>
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
            <LiveCameraStage ref={stage}>
                <CameraPlaceholders count={placeholderCount} visible={!showCameras} />
                {!omeStreams && statusText && (
                    <CameraStatusOverlay>
                        {(status === 'connecting' || status === 'reconnecting') && (
                            <>
                                <CircularProgress variant="indeterminate" size={32} aria-hidden="true" />
                                <ScreenReaderStatus role="status">{TranslateText(statusText)}</ScreenReaderStatus>
                            </>
                        )}
                        {status === 'unavailable' && (
                            <>
                                <Typography role="status">{TranslateText(statusText)}</Typography>
                                <RetryButton onClick={() => retryMediaStream(robotId)}>
                                    {TranslateText('Retry')}
                                </RetryButton>
                            </>
                        )}
                    </CameraStatusOverlay>
                )}
                {!showCameras && cameraKeys.length > 0 && (omeStreams || !statusText) && (
                    <CameraStatusOverlay>
                        <CircularProgress variant="indeterminate" size={32} aria-hidden="true" />
                        <ScreenReaderStatus>{TranslateText('Connecting cameras…')}</ScreenReaderStatus>
                    </CameraStatusOverlay>
                )}
                <LiveCameraRow $visible={showCameras} aria-hidden={!showCameras} inert={!showCameras}>
                    {videoCards}
                    {omeAttemptId !== undefined &&
                        omeStreams?.map((camera) => {
                            const cameraKey = `${robotId}-${camera.cameraId}`
                            const quarterTurn = Math.abs((camera.stream?.rotationDegrees ?? 0) % 180) === 90
                            const aspectRatio = aspectRatios[cameraKey] ?? (quarterTurn ? 9 / 16 : 16 / 9)
                            return (
                                <OmeCameraTile
                                    key={cameraKey}
                                    robotId={robotId}
                                    omeAttemptId={omeAttemptId}
                                    camera={camera}
                                    aspectRatio={aspectRatio}
                                    position={positions[cameraKey]}
                                    onAspectRatioChange={(ratio) => updateAspectRatio(cameraKey, ratio)}
                                />
                            )
                        })}
                </LiveCameraRow>
            </LiveCameraStage>
        </>
    )
}
