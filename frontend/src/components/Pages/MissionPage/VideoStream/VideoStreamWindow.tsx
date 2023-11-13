import { useState } from 'react'
import { Typography } from '@equinor/eds-core-react'
import { FullScreenVideoStreamCard } from './FullScreenVideo'
import { VideoStream } from 'models/VideoStream'
import { VideoStreamCard } from './VideoStreamCards'
import styled from 'styled-components'
import ReactModal from 'react-modal'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

const VideoStreamContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 3rem;
    padding-top: 1rem;
    padding-bottom: 1rem;
`

const VideoFullScreen = styled(ReactModal)`
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    padding-top: 5rem;
`

interface VideoStreamWindowProps {
    videoStreams: VideoStream[]
}

export function VideoStreamWindow({ videoStreams }: VideoStreamWindowProps) {
    const { TranslateText } = useLanguageContext()
    const [fullScreenMode, setFullScreenMode] = useState<boolean>(false)
    const [fullScreenStream, setFullScreenStream] = useState<VideoStream>()

    const toggleFullScreenMode = () => {
        setFullScreenMode(!fullScreenMode)
    }
    const updateFullScreenStream = (videoStream: VideoStream) => {
        setFullScreenStream(videoStream)
        toggleFullScreenMode()
    }

    const videoCards = videoStreams.map(function (videoStream, index) {
        return (
            <VideoStreamCard
                key={index}
                videoStream={videoStream}
                toggleFullScreenMode={toggleFullScreenMode}
                setFullScreenStream={updateFullScreenStream}
            />
        )
    })

    const videoStream = fullScreenStream
    return (
        <>
            <Typography variant="h2">{TranslateText('Camera')}</Typography>
            <VideoStreamContent>
                {fullScreenMode === false && videoCards}
                {videoStream && (
                    <VideoFullScreen isOpen={fullScreenMode} onRequestClose={toggleFullScreenMode}>
                        {FullScreenVideoStreamCard({ videoStream, toggleFullScreenMode })}
                    </VideoFullScreen>
                )}
            </VideoStreamContent>
        </>
    )
}
