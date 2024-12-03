interface IVideoPlayerProps {
    videoStream: MediaStream
    videoStreamName: string
}

export const VideoPlayerSimpleStream = ({ videoStream, videoStreamName }: IVideoPlayerProps) => (
    <video
        autoPlay
        muted
        playsInline
        ref={(video) => {
            if (video) video.srcObject = videoStream
        }}
        style={{ height: '100%', width: '100%' }}
    />
)
