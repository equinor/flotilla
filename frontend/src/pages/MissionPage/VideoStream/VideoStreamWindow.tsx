import { Button, Typography } from '@equinor/eds-core-react'
import { VideoStreamCard } from './VideoStreamCards'
import styled from 'styled-components'
import { useMediaStreamContext } from 'contexts/MediaStreamContext'
import { useEffect } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'

const VideoStreamContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 3rem;
    padding-top: 1rem;
    padding-bottom: 1rem;
`

interface VideoStreamWindowProps {
    robotId: string
}

export const VideoStreamWindow = ({ robotId }: VideoStreamWindowProps) => {
    const { TranslateText } = useLanguageContext()
    const { mediaStreams, acquireMediaStream, retryMediaStream } = useMediaStreamContext()
    useEffect(() => acquireMediaStream(robotId), [robotId, acquireMediaStream])

    const { streams: videoStreams, status } = mediaStreams[robotId] ?? { streams: [], status: 'connecting' }
    const statusText = {
        connecting: 'Connecting',
        reconnecting: 'Reconnecting',
        unavailable: 'Stream unavailable',
        connected: '',
    }[status]
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
            {statusText && <Typography role="status">{TranslateText(statusText)}</Typography>}
            {status === 'unavailable' && (
                <Button onClick={() => retryMediaStream(robotId)}>{TranslateText('Retry')}</Button>
            )}
            <VideoStreamContent>{videoCards}</VideoStreamContent>
        </>
    )
}
