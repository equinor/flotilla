import { Card, Typography, Icon, Button } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { VideoPlayerSimpleStream } from './VideoPlayerSimpleStream'

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

interface IVideoStreamCardProps {
    videoStream: MediaStream
    videoStreamName?: string
    toggleFullScreenMode: VoidFunction
    setFullScreenStream: Function
}

export const VideoStreamCard = ({
    videoStream,
    videoStreamName,
    toggleFullScreenMode,
    setFullScreenStream,
}: IVideoStreamCardProps) => {
    const turnOnFullScreen = () => {
        setFullScreenStream(videoStream)
        toggleFullScreenMode()
    }

    const fullScreenButton = (
        <FullscreenButton color="secondary" onClick={turnOnFullScreen}>
            <Icon name={Icons.Fullscreen} size={32} />
        </FullscreenButton>
    )

    return (
        <VideoCard style={{ boxShadow: tokens.elevation.raised }}>
            <div onDoubleClick={turnOnFullScreen}>
                <StyledVideoSection>
                    <VideoPlayerSimpleStream videoStream={videoStream} />
                    {fullScreenButton}
                </StyledVideoSection>
            </div>
            {videoStreamName && <Typography variant="h5">{videoStreamName}</Typography>}
        </VideoCard>
    )
}
