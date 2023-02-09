import { VideoPlayerOvenPlayer, IsValidOvenPlayerType } from './VideoPlayerOvenPlayer'
import { VideoPlayerSimple } from './VideoPlayerSimple'
import { VideoStream } from 'models/VideoStream'
import styled from 'styled-components'
import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'

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

export function FullScreenVideoStreamCard(videoStream: VideoStream) {
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

    if (IsValidOvenPlayerType({ videoStream })) {
        if (videoStream.shouldRotate) {
            return (
                <FullScreenCardRotated
                    style={{ boxShadow: tokens.elevation.raised, width: rotatedCardWidth() }}
                >
                    <PositionText>
                        <RotateText>
                            <Typography variant="h5">{videoStream.name}</Typography>
                        </RotateText>
                        <VideoPlayerOvenPlayer videoStream={videoStream} />
                    </PositionText>
                </FullScreenCardRotated>
            )
        }
        return (
            <FullScreenCard style={{ boxShadow: tokens.elevation.raised, width: cardWidth() }}>
                <Typography variant="h5">{videoStream.name}</Typography>
                <VideoPlayerOvenPlayer videoStream={videoStream} />
            </FullScreenCard>
        )
    }
    // Rotated stream is not supported for simpleplayer
    return (
        <FullScreenCard style={{ boxShadow: tokens.elevation.raised, width: cardWidth() }}>
            <Typography variant="h5">{videoStream.name}</Typography>
            <VideoPlayerSimple videoStream={videoStream} />
        </FullScreenCard>
    )
}
