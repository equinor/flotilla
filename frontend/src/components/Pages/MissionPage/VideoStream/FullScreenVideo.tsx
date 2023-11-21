import { VideoPlayerOvenPlayer, isValidOvenPlayerType } from './VideoPlayerOvenPlayer'
import { VideoPlayerSimple } from './VideoPlayerSimple'
import { VideoStream } from 'models/VideoStream'
import styled from 'styled-components'
import { Typography, Button, Icon } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Icons } from 'utils/icons'

const FullscreenExitButton = styled(Button)`
    position: absolute;
    bottom: 1px;
    right: 1px;
    width: 32px;
    height: 32px;
    opacity: 0.75;
`

const FullscreenExitButtonRotate = styled(Button)`
    position: absolute;
    bottom: 1px;
    left: 1px;
    width: 32px;
    height: 32px;
    opacity: 0.75;
`

const FullScreenCard = styled.div`
    padding: 1rem;
`

// Styles for rotation
const FullScreenCardRotated = styled.div`
    transform: rotate(270deg);
    padding: 1rem;
`
const RotateText = styled.div`
    writing-mode: vertical-rl;
`

const PositionText = styled.div`
    display: flex;
    flex-direction: row-reverse;
`

interface IFullScreenVideoStreamCardProps {
    videoStream: VideoStream
    toggleFullScreenMode: VoidFunction
}

export function FullScreenVideoStreamCard({ videoStream, toggleFullScreenMode }: IFullScreenVideoStreamCardProps) {
    const cardWidth = () => {
        const availableInnerHeight = window.innerHeight - 9 * 16
        const availableInnerWidth = window.innerWidth - 2 * 16
        const coverageFactor = 0.9
        const aspectRatio = 16 / 9
        return Math.round(
            Math.min(coverageFactor * availableInnerWidth, aspectRatio * coverageFactor * availableInnerHeight)
        )
    }
    const rotatedCardWidth = () => {
        const availableInnerHeight = window.innerHeight - 7.5 * 16
        const availableInnerWidth = window.innerWidth + 0.5 * 16
        const coverageFactor = 0.9
        const aspectRatio = 9 / 16
        return Math.round(
            Math.min(coverageFactor * availableInnerHeight, aspectRatio * coverageFactor * availableInnerWidth)
        )
    }

    const fullScreenExitButton = (shouldRotate270Clockwise: boolean) => {
        if (shouldRotate270Clockwise) {
            return (
                <FullscreenExitButtonRotate color="secondary" onClick={toggleFullScreenMode}>
                    <Icon name={Icons.FullscreenExit} size={32} />
                </FullscreenExitButtonRotate>
            )
        }
        return (
            <FullscreenExitButton color="secondary" onClick={toggleFullScreenMode}>
                <Icon name={Icons.FullscreenExit} size={32} />
            </FullscreenExitButton>
        )
    }

    if (isValidOvenPlayerType(videoStream)) {
        if (videoStream.shouldRotate270Clockwise) {
            return (
                <FullScreenCardRotated style={{ boxShadow: tokens.elevation.raised, width: rotatedCardWidth() }}>
                    <PositionText>
                        <RotateText>
                            <Typography variant="h5">{videoStream.name}</Typography>
                        </RotateText>
                        <VideoPlayerOvenPlayer videoStream={videoStream} />
                    </PositionText>
                    {fullScreenExitButton(true)}
                </FullScreenCardRotated>
            )
        }
        return (
            <FullScreenCard style={{ boxShadow: tokens.elevation.raised, width: cardWidth() }}>
                <Typography variant="h5">{videoStream.name}</Typography>
                <VideoPlayerOvenPlayer videoStream={videoStream} />
                {fullScreenExitButton(false)}
            </FullScreenCard>
        )
    }
    // Rotated stream is not supported for simpleplayer
    return (
        <FullScreenCard style={{ boxShadow: tokens.elevation.raised, width: cardWidth() }}>
            <Typography variant="h5">{videoStream.name}</Typography>
            <VideoPlayerSimple videoStream={videoStream} />
            {fullScreenExitButton(false)}
        </FullScreenCard>
    )
}
