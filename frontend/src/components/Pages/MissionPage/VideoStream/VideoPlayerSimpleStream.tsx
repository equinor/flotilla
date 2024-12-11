export const VideoPlayerSimpleStream = ({ videoStream }: { videoStream: MediaStream }) => (
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
