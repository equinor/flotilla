import { useRef } from 'react'
import { CameraFullscreenButton } from './CameraFullscreenButton'
import { CameraViewport } from './CameraLayout'
import { VideoPlayerSimpleStream } from './VideoPlayerSimpleStream'
import { useVideoAspectRatio } from './useVideoAspectRatio'

interface VideoStreamCardProps {
    videoStream: MediaStream
    aspectRatio: number
    onAspectRatioChange: (aspectRatio: number) => void
}

export const VideoStreamCard = ({ videoStream, aspectRatio, onAspectRatioChange }: VideoStreamCardProps) => {
    const container = useRef<HTMLDivElement>(null)
    useVideoAspectRatio(container, 0, onAspectRatioChange)

    const enterFullscreen = () => {
        void container.current?.querySelector('video')?.requestFullscreen?.()
    }

    return (
        <CameraViewport ref={container} $aspectRatio={aspectRatio} onDoubleClick={enterFullscreen}>
            <VideoPlayerSimpleStream videoStream={videoStream} />
            <CameraFullscreenButton onClick={enterFullscreen} />
        </CameraViewport>
    )
}
