import { Button, Card, Icon } from '@equinor/eds-core-react'
import { fullscreen, videocam_off } from '@equinor/eds-icons'
import styled from 'styled-components'

import { VideoStream } from 'models/VideoStream'
import VideoPlayer from './Video'

Icon.add({ fullscreen, videocam_off })

interface VideoSectionProps {
    videoStream: VideoStream
    toggleFullScreenModeFunction: VoidFunction
}

const StyledVideoSection = styled(Card)`
    height: 95%;
    display: inline-flex;
`

const FullscreenButton = styled(Button)`
    position: absolute;
    bottom: 8px;
    right: 8px;
    width: 32px;
    height: 32px;
    opacity: 0.75;
`

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

export function VideoSection({ videoStream, toggleFullScreenModeFunction }: VideoSectionProps) {
    const videoJsOptions = {
        sources: [
            {
                src: videoStream.url,
                type: 'application/x-mpegURL',
            },
        ],
    }

    return (
        <StyledVideoSection>
            {ValidateVideoStream(videoStream) && (
                    <VideoPlayer options={videoJsOptions} />
                ) && (
                    <FullscreenButton color="secondary" onClick={toggleFullScreenModeFunction}>
                        <Icon name="fullscreen" size={32} />
                    </FullscreenButton>
                )}
            {!ValidateVideoStream(videoStream) && <NoVideoPlaceholder name="videocam_off" />}
        </StyledVideoSection>
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
