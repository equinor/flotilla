import { Card, Typography, Icon, Button } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { VideoPlayerOvenPlayer, IsValidOvenPlayerType } from './VideoPlayerOvenPlayer'
import { VideoPlayerSimple } from './VideoPlayerSimple'
import { VideoStream } from 'models/VideoStream'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const FullscreenButton = styled(Button)`
    position: absolute;
    bottom: 8px;
    right: 8px;
    width: 32px;
    height: 32px;
    opacity: 0.75;
`

const VideoCard = styled(Card)`
    padding: 1rem;
    height: 15rem;
    width: 20rem;
`

const StyledVideoSection = styled.div`
    height: 10rem;
`

const StyledVideoSectionRotated = styled.div`
    height: 10rem;
    width: 10rem;
`

const Rotate = styled.div`
    transform: rotate(270deg);
    position: relative;
    left: 4rem;
    bottom: 4rem;
`

interface IVideoStreamCardProps {
    videoStream: VideoStream
    toggleFullScreenMode: VoidFunction
    setFullScreenStream: Function
}

export function VideoStreamCard({ videoStream, toggleFullScreenMode, setFullScreenStream }: IVideoStreamCardProps) {
    var videoPlayer = null

    const turnOnFullScreen = () => {
        setFullScreenStream(videoStream)
        toggleFullScreenMode()
    }

    const fullScreenButton = (
        <FullscreenButton color="secondary" onClick={turnOnFullScreen}>
            <Icon name={Icons.Fullscreen} size={32} />
        </FullscreenButton>
    )

    if (IsValidOvenPlayerType({ videoStream })) {
        if (videoStream.shouldRotate270Clockwise) {
            videoPlayer = (
                <StyledVideoSectionRotated>
                    <Rotate>
                        <VideoPlayerOvenPlayer videoStream={videoStream} />
                    </Rotate>
                    {fullScreenButton}
                </StyledVideoSectionRotated>
            )
        } else {
            videoPlayer = (
                <StyledVideoSection>
                    <VideoPlayerOvenPlayer videoStream={videoStream} />
                    {fullScreenButton}
                </StyledVideoSection>
            )
        }
    } else {
        // Rotated stream is not supported for simpleplayer
        videoPlayer = (
            <StyledVideoSection>
                <VideoPlayerSimple videoStream={videoStream} />
                {fullScreenButton}
            </StyledVideoSection>
        )
    }

    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <div onDoubleClick={turnOnFullScreen}>{videoPlayer}</div>
            <Typography variant="h5">{videoStream.name}</Typography>
        </VideoCard>
    )
}
