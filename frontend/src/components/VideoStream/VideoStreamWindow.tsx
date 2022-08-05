import { Button, Card, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { fullscreen } from '@equinor/eds-icons'
import styled from 'styled-components'
import { VideoStream } from 'models/VideoStream'
import { useState } from 'react'

interface VideoStreamProps {
    videoStream: VideoStream
}

const VideoCard = styled(Card)`
    width: 400px;
    padding: 16px;
`

const VideoSection = styled(Card)`
    display: inline-flex;
`

const FullscreenButton = styled(Button)`
    position: absolute;
    position: absolute;
    bottom: 8px;
    right: 8px;
    width: 32px;
    height: 32px;
    opacity: 0.75;
`

Icon.add({ fullscreen })

export function VideoStreamWindow({ videoStream }: VideoStreamProps) {
    const [fullScreenMode, setFullScreenMode] = useState<boolean>(false)
    const toggleFullScreenMode = () => {
        setFullScreenMode(!fullScreenMode)
    }

    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            <VideoSection>
                <img src={videoStream.url} alt="robot video stream" />
                <FullscreenButton color="secondary" onClick={toggleFullScreenMode}>
                    <Icon name="fullscreen" size={32} />
                </FullscreenButton>
            </VideoSection>
            <Typography variant="h5">{videoStream.name}</Typography>
        </VideoCard>
    )
}
