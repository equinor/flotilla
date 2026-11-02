import { Card, Typography, Icon, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { VideoPlayerSimpleStream } from './VideoPlayerSimpleStream'
import { useRef } from 'react'
import { useVideoAspectRatio } from './useVideoAspectRatio'

const FullscreenButton = styled(Button)`
    position: absolute;
    bottom: 8px;
    right: 8px;
    width: 32px;
    height: 32px;
    opacity: 0.75;
`
const VideoCard = styled(Card)`
    padding: 0;
    height: auto;
    width: 100%;
    box-shadow: none;
`
const StyledVideoSection = styled.div<{ $aspectRatio: number }>`
    position: relative;
    aspect-ratio: ${({ $aspectRatio }) => $aspectRatio};

    video {
        display: block;
        object-fit: contain;
    }
`

interface IVideoStreamCardProps {
    videoStream: MediaStream
    videoStreamName?: string
    videoStreamId: string
    onAspectRatioChange?: (aspectRatio: number) => void
}

export const VideoStreamCard = ({
    videoStream,
    videoStreamName,
    videoStreamId,
    onAspectRatioChange,
}: IVideoStreamCardProps) => {
    const container = useRef<HTMLDivElement>(null)
    const aspectRatio = useVideoAspectRatio(container, 0, onAspectRatioChange)
    const setFullScreen = () => {
        const elem = document.getElementById(videoStreamId)
        if (elem && elem.requestFullscreen) {
            elem.requestFullscreen()
        }
    }
    const fullScreenButton = (
        <FullscreenButton color="secondary" onClick={setFullScreen} aria-label="Enter fullscreen">
            <Icon name={Icons.Fullscreen} size={32} />
        </FullscreenButton>
    )

    return (
        <VideoCard>
            <div onDoubleClick={setFullScreen}>
                <StyledVideoSection ref={container} $aspectRatio={aspectRatio}>
                    <VideoPlayerSimpleStream videoStream={videoStream} id={videoStreamId} />
                    {fullScreenButton}
                </StyledVideoSection>
            </div>
            {videoStreamName && <Typography variant="h5">{videoStreamName}</Typography>}
        </VideoCard>
    )
}
