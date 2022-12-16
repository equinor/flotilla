import { Card, Typography, Icon } from '@equinor/eds-core-react'
import { videocam_off } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

import { VideoStream } from 'models/VideoStream'
import VideoPlayer from './Video'

Icon.add({ videocam_off })

const NoVideoPlaceholder = styled(Icon)`
    position: absolute;
    top: 50%;
    left: 50%;
    margin-right: -50%;
    transform: translate(-50%, -50%);
    width: 50%;
    height: 50%;
    opacity: 0.4;
`

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
                {ValidateVideoStream(videoStream) && <VideoPlayer options={videoJsOptions} />}
                {!ValidateVideoStream(videoStream) && <NoVideoPlaceholder name="videocam_off" />}
            </StyledVideoSection>
            <Typography variant="h5">{videoStream.name}</Typography>
        </VideoCard>
    )
}

function ValidateVideoStream(videoStream: VideoStream) {
    const stream = new Image()
    stream.src = videoStream.url
    if (stream.naturalHeight === 0) {
        return false
    }
    return true
}
