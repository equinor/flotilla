import { Button, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMediaStreamContext } from 'contexts/MediaStreamContext'
import { OmeStreamState } from 'contexts/OmeStreamManager'
import styled from 'styled-components'
import { CameraViewport } from './CameraLayout'
import { mediaStreamStatusLabels } from './mediaStreamStatusLabels'
import { OmeVideoCard } from './OmeVideoCard'

const CameraStatus = styled.div`
    position: absolute;
    inset: 0;
    z-index: 2;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: ${tokens.spacings.comfortable.medium_small};
    padding: ${tokens.spacings.comfortable.medium_small};
    background: ${tokens.colors.ui.background__light.hex};
    text-align: center;
`

interface OmeCameraTileProps {
    robotId: string
    omeAttemptId: number
    camera: OmeStreamState
    aspectRatio: number
    onAspectRatioChange: (aspectRatio: number) => void
}

export const OmeCameraTile = ({
    robotId,
    omeAttemptId,
    camera,
    aspectRatio,
    onAspectRatioChange,
}: OmeCameraTileProps) => {
    const { TranslateText } = useLanguageContext()
    const { markOmeStreamPlaying, markOmeStreamStalled, reconnectOmeStream, retryOmeStream } = useMediaStreamContext()

    return (
        <>
            <Typography variant="h5" lines={1}>
                {camera.cameraId}
            </Typography>
            <CameraViewport $aspectRatio={aspectRatio}>
                {camera.stream && (
                    <OmeVideoCard
                        key={camera.attemptId}
                        stream={camera.stream}
                        onAspectRatioChange={onAspectRatioChange}
                        onPlaying={() => markOmeStreamPlaying(robotId, omeAttemptId, camera.cameraId, camera.attemptId)}
                        onStalled={() => markOmeStreamStalled(robotId, omeAttemptId, camera.cameraId, camera.attemptId)}
                        onDisconnect={() =>
                            reconnectOmeStream(robotId, omeAttemptId, camera.cameraId, camera.attemptId)
                        }
                    />
                )}
                {camera.status !== 'connected' && (
                    <CameraStatus>
                        <Typography role="status" color={tokens.colors.text.static_icons__default.hex}>
                            {TranslateText(mediaStreamStatusLabels[camera.status])}
                        </Typography>
                        {camera.status === 'unavailable' && (
                            <Button onClick={() => retryOmeStream(robotId, omeAttemptId, camera.cameraId)}>
                                {TranslateText('Retry')}
                            </Button>
                        )}
                    </CameraStatus>
                )}
            </CameraViewport>
        </>
    )
}
