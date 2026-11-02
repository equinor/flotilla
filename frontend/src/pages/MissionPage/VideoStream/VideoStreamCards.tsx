import { Card, Typography, Icon, Button } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
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
const VideoCard = styled(Card)<{ $compact: boolean }>`
    padding: ${({ $compact }) => ($compact ? '0' : '1rem')};
    height: ${({ $compact }) => ($compact ? 'auto' : '15rem')};
    width: ${({ $compact }) => ($compact ? '100%' : '20rem')};
`
const StyledVideoSection = styled.div<{ $compact: boolean; $aspectRatio: number }>`
    position: relative;
    height: ${({ $compact }) => ($compact ? 'auto' : '10rem')};
    ${({ $compact, $aspectRatio }) => $compact && `aspect-ratio: ${$aspectRatio};`}

    video {
        display: block;
        object-fit: contain;
    }
`

interface IVideoStreamCardProps {
    videoStream: MediaStream
    videoStreamName?: string
    videoStreamId: string
    compact?: boolean
    onAspectRatioChange?: (aspectRatio: number) => void
}

export const VideoStreamCard = ({
    videoStream,
    videoStreamName,
    videoStreamId,
    compact = false,
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
        <VideoCard $compact={compact} style={{ boxShadow: compact ? 'none' : tokens.elevation.raised }}>
            <div onDoubleClick={setFullScreen}>
                <StyledVideoSection ref={container} $compact={compact} $aspectRatio={aspectRatio}>
                    <VideoPlayerSimpleStream videoStream={videoStream} id={videoStreamId} />
                    {fullScreenButton}
                </StyledVideoSection>
            </div>
            {videoStreamName && <Typography variant="h5">{videoStreamName}</Typography>}
        </VideoCard>
    )
}
