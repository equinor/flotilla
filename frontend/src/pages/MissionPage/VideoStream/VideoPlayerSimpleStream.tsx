export const VideoPlayerSimpleStream = ({ videoStream, id }: { videoStream: MediaStream; id: string }) => (
    <video
        id={id}
        autoPlay
        muted
        playsInline
        ref={(video) => {
            if (video) video.srcObject = videoStream
        }}
        style={{ height: '100%', width: '100%' }}
    />
)
