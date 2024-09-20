import ReactPlayer from 'react-player/lazy'

interface IVideoPlayerProps {
    videoStream: MediaStream
    videoStreamName: string
}

export const VideoPlayerSimpleStream = ({ videoStream, videoStreamName }: IVideoPlayerProps) => (
    <video autoPlay ref={(video) => {if (video) video.srcObject = videoStream;}} style={{ height: "100%", width: "100%" }}/>
)
