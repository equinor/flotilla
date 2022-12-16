import { useRef, useEffect, FC } from 'react'
import videojs from 'video.js'

// Styles
import 'video.js/dist/video-js.css'

interface IVideoPlayerProps {
    options: videojs.PlayerOptions
}

const initialOptions: videojs.PlayerOptions = {
    controls: true,
    fluid: true,
    muted: true,
    controlBar: {
        volumePanel: {
            inline: false,
        },
    },
}

const VideoPlayer: FC<IVideoPlayerProps> = ({ options }) => {
    const player = useRef<videojs.Player>()

    useEffect(() => {
        player.current = videojs('videoJSplayer', {
            ...initialOptions,
            ...options,
        }).ready(function () {})
        return () => {
            if (player.current) {
                player.current.dispose()
            }
        }
    }, [options])

    return <video id="videoJSplayer" className="video-js" />
}

export default VideoPlayer
