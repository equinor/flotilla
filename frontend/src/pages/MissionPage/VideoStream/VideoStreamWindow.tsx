import { Button, CircularProgress, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMediaStreamContext } from 'contexts/MediaStreamContext'
import { useEffect, useMemo, useState } from 'react'
import styled from 'styled-components'
import { CAMERA_GAP_REM, Camera, Cameras } from './CameraLayout'
import { mediaStreamStatusLabels } from './mediaStreamStatusLabels'
import { OmeCameraTile } from './OmeCameraTile'
import { VideoStreamCard } from './VideoStreamCards'

const LiveCameras = styled(Cameras)`
    flex-wrap: wrap;
    min-width: 0;
    padding-block: ${CAMERA_GAP_REM}rem;
`

const CAMERA_WRAP_HEIGHT_REM = 6.75
const CAMERA_MAX_HEIGHT_REM = 15

const LiveCamera = styled(Camera)`
    flex-basis: ${({ $aspectRatio }) => $aspectRatio * CAMERA_WRAP_HEIGHT_REM}rem;
    max-width: ${({ $aspectRatio }) => $aspectRatio * CAMERA_MAX_HEIGHT_REM}rem;
`

const ConnectionStatus = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: ${CAMERA_GAP_REM}rem;
    margin-top: ${CAMERA_GAP_REM}rem;
`

interface VideoStreamWindowProps {
    robotId: string
}

export const VideoStreamWindow = ({ robotId }: VideoStreamWindowProps) => {
    const { TranslateText } = useLanguageContext()
    const { mediaStreams, acquireMediaStream, retryMediaStream } = useMediaStreamContext()
    const [aspectRatios, setAspectRatios] = useState<Record<string, number>>({})
    useEffect(() => acquireMediaStream(robotId), [robotId, acquireMediaStream])

    const updateAspectRatio = (key: string, aspectRatio: number) => {
        setAspectRatios((ratios) => (ratios[key] === aspectRatio ? ratios : { ...ratios, [key]: aspectRatio }))
    }

    const { streams, omeStreams, omeAttemptId, status = 'connecting' } = mediaStreams[robotId] ?? {}
    const statusText = mediaStreamStatusLabels[status]
    const liveKitStreams = useMemo(
        () => streams?.map((track) => ({ id: track.id, stream: new MediaStream([track]) })) ?? [],
        [streams]
    )
    const liveKitCameras = liveKitStreams.map(({ id, stream }) => {
        const key = `${robotId}-livekit-${id}`
        const aspectRatio = aspectRatios[key] ?? 16 / 9
        return {
            key,
            aspectRatio,
            player: (
                <VideoStreamCard
                    videoStream={stream}
                    aspectRatio={aspectRatio}
                    onAspectRatioChange={(ratio) => updateAspectRatio(key, ratio)}
                />
            ),
        }
    })
    const omeCameras =
        omeAttemptId === undefined
            ? []
            : (omeStreams ?? []).map((camera) => {
                  const key = `${robotId}-ome-${camera.cameraId}`
                  const quarterTurn = Math.abs((camera.stream?.rotationDegrees ?? 0) % 180) === 90
                  const aspectRatio = aspectRatios[key] ?? (quarterTurn ? 9 / 16 : 16 / 9)
                  return {
                      key,
                      aspectRatio,
                      player: (
                          <OmeCameraTile
                              robotId={robotId}
                              omeAttemptId={omeAttemptId}
                              camera={camera}
                              aspectRatio={aspectRatio}
                              onAspectRatioChange={(ratio) => updateAspectRatio(key, ratio)}
                          />
                      ),
                  }
              })
    const cameras = [...liveKitCameras, ...omeCameras]

    return (
        <>
            <Typography variant="h2">{TranslateText('Camera')}</Typography>
            {!omeStreams && statusText && (
                <ConnectionStatus>
                    {status !== 'unavailable' && (
                        <CircularProgress variant="indeterminate" size={32} aria-hidden="true" />
                    )}
                    <Typography role="status">{TranslateText(statusText)}</Typography>
                    {status === 'unavailable' && (
                        <Button onClick={() => retryMediaStream(robotId)}>{TranslateText('Retry')}</Button>
                    )}
                </ConnectionStatus>
            )}
            <LiveCameras>
                {cameras.map(({ key, aspectRatio, player }) => (
                    <LiveCamera key={key} $aspectRatio={aspectRatio}>
                        {player}
                    </LiveCamera>
                ))}
            </LiveCameras>
        </>
    )
}
