import { Button, Card, Icon } from '@equinor/eds-core-react'
import { fullscreen } from '@equinor/eds-icons'
import styled from 'styled-components'

import { VideoStream } from 'models/VideoStream'

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

Icon.add({ fullscreen })

export function VideoSection({ videoStream, toggleFullScreenModeFunction }: VideoSectionProps) {
    return (
        <StyledVideoSection>
            <video autoPlay src={videoStream.url} width="100%" height="100%" />
            <FullscreenButton color="secondary" onClick={toggleFullScreenModeFunction}>
                <Icon name="fullscreen" size={32} />
            </FullscreenButton>
        </StyledVideoSection>
    )
}
