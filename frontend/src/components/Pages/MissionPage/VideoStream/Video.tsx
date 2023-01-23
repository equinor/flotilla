import { useEffect } from 'react'

import OvenPlayer from 'ovenplayer'

// Styles
import 'video.js/dist/video-js.css'
import { VideoStream } from 'models/VideoStream'

interface IVideoPlayerProps {
    videoStream: VideoStream
}

export function VideoPlayer({ videoStream }: IVideoPlayerProps) {
    useEffect(() => {
        const player = OvenPlayer.create(videoStream.id, {
            sources: [
                {
                    label: videoStream.name,
                    type: 'webrtc',
                    file: videoStream.url,
                },
            ],
        })
    }, [])

    return <div id={videoStream.id} />
}
