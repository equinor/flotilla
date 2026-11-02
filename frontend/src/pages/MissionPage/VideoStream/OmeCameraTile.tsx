import { Button, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMediaStreamContext } from 'contexts/MediaStreamContext'
import { OmeStreamState } from 'contexts/OmeStreamManager'
import { CameraPosition } from './cameraPositions'
import { LiveCamera, LiveCameraStatus, LiveCameraViewport } from './LiveCameraLayout'
import { mediaStreamStatusLabels } from './mediaStreamStatusLabels'
import { OmeVideoCard } from './OmeVideoCard'

interface OmeCameraTileProps {
    robotId: string
    omeAttemptId: number
    camera: OmeStreamState
    aspectRatio: number
    position?: CameraPosition
    onAspectRatioChange: (aspectRatio: number) => void
}

export const OmeCameraTile = ({
    robotId,
    omeAttemptId,
    camera,
    aspectRatio,
    position,
    onAspectRatioChange,
}: OmeCameraTileProps) => {
    const { TranslateText } = useLanguageContext()
    const { markOmeStreamPlaying, markOmeStreamStalled, reconnectOmeStream, retryOmeStream } = useMediaStreamContext()

    return (
        <LiveCamera $position={position}>
            <Typography variant="h5" lines={1}>
                {camera.cameraId}
            </Typography>
            <LiveCameraViewport $aspectRatio={aspectRatio}>
                {camera.stream && (
                    <OmeVideoCard
                        key={camera.attemptId}
                        stream={camera.stream}
                        aspectRatio={aspectRatio}
                        onAspectRatioChange={onAspectRatioChange}
                        onPlaying={() => markOmeStreamPlaying(robotId, omeAttemptId, camera.cameraId, camera.attemptId)}
                        onStalled={() => markOmeStreamStalled(robotId, omeAttemptId, camera.cameraId, camera.attemptId)}
                        onDisconnect={() =>
                            reconnectOmeStream(robotId, omeAttemptId, camera.cameraId, camera.attemptId)
                        }
                    />
                )}
                {camera.status !== 'connected' && (
                    <LiveCameraStatus>
                        <Typography role="status" color={tokens.colors.text.static_icons__primary_white.hex}>
                            {TranslateText(mediaStreamStatusLabels[camera.status])}
                        </Typography>
                        {camera.status === 'unavailable' && (
                            <Button onClick={() => retryOmeStream(robotId, omeAttemptId, camera.cameraId)}>
                                {TranslateText('Retry')}
                            </Button>
                        )}
                    </LiveCameraStatus>
                )}
            </LiveCameraViewport>
        </LiveCamera>
    )
}
