import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

import { VideoStream } from 'models/VideoStream'
import VideoPlayer from './Video'

const VideoCard = styled(Card)`
    padding: 16px;
    height: 15rem;
    width: 20rem;
`

const StyledVideoSection = styled(Card)`
    height: 95%;
    display: inline-flex;
`

interface VideoStreamWindowProps {
    videoStream: VideoStream
}

export function VideoStreamWindow({ videoStream }: VideoStreamWindowProps) {
    const videoJsOptions = {
        sources: [
            {
                src: videoStream.url,
                type: 'application/x-mpegURL',
            },
        ],
    }

    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <StyledVideoSection>
                <VideoPlayer options={videoJsOptions} />
            </StyledVideoSection>
            <Typography variant="h5">{videoStream.name}</Typography>
        </VideoCard>
    )
}
