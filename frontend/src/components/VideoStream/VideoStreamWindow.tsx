import { Card, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { fullscreen } from '@equinor/eds-icons'
import styled from 'styled-components'
import { useState } from 'react'
import ReactModal from 'react-modal'

import { VideoStream } from 'models/VideoStream'
import { VideoSection } from './VideoSection'

interface VideoStreamWindowProps {
    videoStream: VideoStream
}
const VideoCard = styled(Card)`
    padding: 16px;
`

const VideoFullScreen = styled(ReactModal)`
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
`

Icon.add({ fullscreen })

export function VideoStreamWindow({ videoStream }: VideoStreamWindowProps) {
    const [fullScreenMode, setFullScreenMode] = useState<boolean>(false)

    const toggleFullScreenMode = () => {
        setFullScreenMode(!fullScreenMode)
    }

    return (
        <>
            <VideoCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
                <VideoSection videoStream={videoStream} toggleFullScreenModeFunction={toggleFullScreenMode} />
                <Typography variant="h5">{videoStream.name}</Typography>
            </VideoCard>
            <VideoFullScreen
                isOpen={fullScreenMode}
                onRequestClose={() => {
                    setFullScreenMode(false)
                }}
            >
                <VideoCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
                    <Typography variant="h5">{videoStream.name}</Typography>
                    <VideoSection videoStream={videoStream} toggleFullScreenModeFunction={toggleFullScreenMode} />
                </VideoCard>
            </VideoFullScreen>
        </>
    )
}
