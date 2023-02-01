import { VideoStream } from 'models/VideoStream'

interface IVideoPlayerProps {
    videoStream: VideoStream
}

export function VideoPlayerSimple({ videoStream }: IVideoPlayerProps) {
    return <img src={videoStream.url} width="100%" height="100%" alt={videoStream.name + ' video stream'} />
}
