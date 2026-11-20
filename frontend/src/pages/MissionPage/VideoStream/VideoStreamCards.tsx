import { Icon, Button, Tooltip } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { VideoPlayerSimpleStream } from './VideoPlayerSimpleStream'
import { useRef } from 'react'
import { useVideoAspectRatio } from './useVideoAspectRatio'

const FullscreenButton = styled(Button)`
    position: absolute;
    bottom: ${tokens.spacings.comfortable.small};
    right: ${tokens.spacings.comfortable.small};
    opacity: 0.75;
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
    videoStreamId: string
    onAspectRatioChange?: (aspectRatio: number) => void
}

export const VideoStreamCard = ({ videoStream, videoStreamId, onAspectRatioChange }: IVideoStreamCardProps) => {
    const container = useRef<HTMLDivElement>(null)
    const aspectRatio = useVideoAspectRatio(container, 0, onAspectRatioChange)
    const setFullScreen = () => {
        const elem = document.getElementById(videoStreamId)
        if (elem && elem.requestFullscreen) {
            elem.requestFullscreen()
        }
    }
    return (
        <StyledVideoSection ref={container} $aspectRatio={aspectRatio} onDoubleClick={setFullScreen}>
            <VideoPlayerSimpleStream videoStream={videoStream} id={videoStreamId} />
            <Tooltip title="Enter fullscreen" placement="top">
                <FullscreenButton
                    variant="contained_icon"
                    color="secondary"
                    onClick={setFullScreen}
                    aria-label="Enter fullscreen"
                >
                    <Icon name={Icons.Fullscreen} size={24} />
                </FullscreenButton>
            </Tooltip>
        </StyledVideoSection>
    )
}
