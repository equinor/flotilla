import { Button, Typography } from '@equinor/eds-core-react'
import { VideoStreamCard } from './VideoStreamCards'
import styled from 'styled-components'
import { useMediaStreamContext } from 'contexts/MediaStreamContext'
import { useEffect } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { OmeVideoCard } from './OmeVideoCard'

const RetryButton = styled(Button)`
    width: fit-content;
`

const VideoStreamContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 3rem;
    padding-top: 1rem;
    padding-bottom: 1rem;
`

const OmeCamera = styled.div`
    width: min(100%, 32rem);
    min-width: 0;
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
    const videoCards = videoStreams.map((videoStream, index) => (
        <VideoStreamCard
            key={videoStream.id}
            videoStream={new MediaStream([videoStream])}
            videoStreamName={undefined}
            videoStreamId={'videostreamid-' + index}
        />
    ))

    return (
        <>
            <Typography variant="h2">{TranslateText('Camera')}</Typography>
            {!omeStreams && statusText && <Typography role="status">{TranslateText(statusText)}</Typography>}
            {!omeStreams && status === 'unavailable' && (
                <RetryButton onClick={() => retryMediaStream(robotId)}>{TranslateText('Retry')}</RetryButton>
            )}
            <VideoStreamContent>
                {videoCards}
                {omeAttemptId !== undefined &&
                    omeStreams?.map((camera) => (
                        <OmeCamera key={`${omeAttemptId}-${camera.role}`}>
                            <Typography variant="h5">{camera.role}</Typography>
                            {camera.stream && (
                                <OmeVideoCard
                                    key={camera.attemptId}
                                    stream={camera.stream}
                                    onPlaying={() =>
                                        markOmeStreamPlaying(robotId, omeAttemptId, camera.role, camera.attemptId)
                                    }
                                    onStalled={() =>
                                        markOmeStreamStalled(robotId, omeAttemptId, camera.role, camera.attemptId)
                                    }
                                    onDisconnect={() =>
                                        reconnectOmeStream(robotId, omeAttemptId, camera.role, camera.attemptId)
                                    }
                                />
                            )}
                            {camera.status !== 'connected' && (
                                <Typography role="status">{TranslateText(statusLabels[camera.status])}</Typography>
                            )}
                            {camera.status === 'unavailable' && (
                                <Button onClick={() => retryOmeStream(robotId, omeAttemptId, camera.role)}>
                                    {TranslateText('Retry')}
                                </Button>
                            )}
                        </OmeCamera>
                    ))}
            </VideoStreamContent>
        </>
    )
}
