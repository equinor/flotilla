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
    const liveKitCameras = liveKitStreams.map(({ id, stream }) => {
        const key = `${robotId}-${id}`
        return { key, stream, aspectRatio: aspectRatios[key] ?? 16 / 9 }
    })
    const omeCameras = (omeStreams ?? []).map((camera) => {
        const key = `${robotId}-${camera.cameraId}`
        const quarterTurn = Math.abs((camera.stream?.rotationDegrees ?? 0) % 180) === 90
        return { key, camera, aspectRatio: aspectRatios[key] ?? (quarterTurn ? 9 / 16 : 16 / 9) }
    })
    const cameras = [...liveKitCameras, ...omeCameras]
    const cameraKeys = cameras.map(({ key }) => key)
    const isRevealed = useCameraReveal(robotId, cameraKeys, aspectRatios)
    const positions = useCameraPositions(stage, cameras)
    const showCameras = isRevealed && cameraKeys.every((key) => positions[key] !== undefined)
    const placeholderCount = omeStreams?.length ?? (liveKitStreams.length || 5)
    const waitingForDimensions = !showCameras && cameraKeys.length > 0 && (!!omeStreams || !statusText)
    const isConnecting = !omeStreams && (status === 'connecting' || status === 'reconnecting')

    return (
        <>
            <Typography variant="h2">{TranslateText('Camera')}</Typography>
            <LiveCameraStage ref={stage}>
                <CameraPlaceholders count={placeholderCount} visible={!showCameras} />
                {(isConnecting || waitingForDimensions) && (
                    <CameraStatusOverlay>
                        <CircularProgress variant="indeterminate" size={32} aria-hidden="true" />
                        <ScreenReaderStatus role={isConnecting ? 'status' : undefined}>
                            {TranslateText(isConnecting ? statusText : 'Connecting cameras…')}
                        </ScreenReaderStatus>
                    </CameraStatusOverlay>
                )}
                {!omeStreams && status === 'unavailable' && (
                    <CameraStatusOverlay>
                        <Typography role="status">{TranslateText(statusText)}</Typography>
                        <Button onClick={() => retryMediaStream(robotId)}>{TranslateText('Retry')}</Button>
                    </CameraStatusOverlay>
                )}
                <LiveCameraRow $visible={showCameras} aria-hidden={!showCameras} inert={!showCameras}>
                    {liveKitCameras.map(({ key, stream }) => (
                        <LiveCamera key={key} $position={positions[key]}>
                            <VideoStreamCard
                                videoStream={stream}
                                videoStreamId={key}
                                onAspectRatioChange={(ratio) => updateAspectRatio(key, ratio)}
                            />
                        </LiveCamera>
                    ))}
                    {omeAttemptId !== undefined &&
                        omeCameras.map(({ key, camera, aspectRatio }) => (
                            <OmeCameraTile
                                key={key}
                                robotId={robotId}
                                omeAttemptId={omeAttemptId}
                                camera={camera}
                                aspectRatio={aspectRatio}
                                position={positions[key]}
                                onAspectRatioChange={(ratio) => updateAspectRatio(key, ratio)}
                            />
                        ))}
                </LiveCameraRow>
            </LiveCameraStage>
        </>
    )
}
