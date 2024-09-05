import ReactPlayer from 'react-player/lazy'

interface IVideoPlayerProps {
    videoStream: MediaStream
    videoStreamName: string
}

export const VideoPlayerSimpleStream = ({ videoStream, videoStreamName }: IVideoPlayerProps) => (
    <ReactPlayer url={videoStream} width="100%" height="100%" alt={videoStreamName + ' video stream'} />
)
