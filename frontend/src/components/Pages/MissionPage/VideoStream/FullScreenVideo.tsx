import styled from 'styled-components'
import { Typography, Button, Icon } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Icons } from 'utils/icons'
import { VideoPlayerSimpleStream } from './VideoPlayerSimpleStream'

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
    box-shadow: ${tokens.elevation.raised};
    width: 100%;
    background-color: white;
`

interface IFullScreenVideoStreamCardProps {
    videoStream: MediaStream
    videoStreamName?: string
    toggleFullScreenMode: VoidFunction
}

export const FullScreenVideoStreamCard = ({
    videoStream,
    videoStreamName,
    toggleFullScreenMode,
}: IFullScreenVideoStreamCardProps) => {
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

    // Rotated stream is not supported for simpleplayer
    return (
        <FullScreenCard>
            {videoStreamName && <Typography variant="h5">{videoStreamName}</Typography>}
            <VideoPlayerSimpleStream videoStream={videoStream} />
            {fullScreenExitButton(false)}
        </FullScreenCard>
    )
}
