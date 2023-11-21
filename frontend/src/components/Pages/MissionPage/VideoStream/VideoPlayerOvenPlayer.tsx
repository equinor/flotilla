import { useEffect } from 'react'

// import OvenPlayer from 'ovenplayer'

// Styles
import 'video.js/dist/video-js.css'
import { VideoStream } from 'models/VideoStream'

interface IVideoPlayerProps {
    videoStream: VideoStream
}

// TODO: Video player is not used at the moment, commented out for now
export function VideoPlayerOvenPlayer({ videoStream }: IVideoPlayerProps) {
    useEffect(() => {
        // const aspectRatio = videoStream.shouldRotate270Clockwise ? '9:16' : '16:9'
        switch (videoStream.type) {
            case 'webrtc':
            case 'hls':
            case 'llhls':
            case 'dash':
            case 'lldash':
            case 'mp4':
            // const player = OvenPlayer.create(videoStream.id, {
            //     aspectRatio: aspectRatio,
            //     controls: false,
            //     mute: true,
            //     autoStart: true,
            //     expandFullScreenUI: false,
            //     sources: [
            //         {
            //             label: videoStream.name,
            //             type: videoStream.type,
            //             file: videoStream.url,
            //         },
            //     ],
            // })
        }
    }, [videoStream.id, videoStream.name, videoStream.shouldRotate270Clockwise, videoStream.type, videoStream.url])

    return <div id={videoStream.id} />
}

export const isValidOvenPlayerType = (videoStream: VideoStream) => {
    const validTypes = ['webrtc', 'hls', 'llhls', 'dash', 'lldash', 'mp4']
    return validTypes.includes(videoStream.type)
}
