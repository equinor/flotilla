import { Card } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { VideoStream } from 'models/VideoStream'

interface VideoStreamProps {
    videoStream: VideoStream
}

const VideoCard = styled(Card)`
    width: 400px;
    height: 225px;
`

export function VideoStreamWindow({ videoStream }: VideoStreamProps) {
    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            <img src={videoStream.url} />
        </VideoCard>
    )
}
