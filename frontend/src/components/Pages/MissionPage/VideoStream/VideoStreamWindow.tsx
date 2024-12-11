import { useState } from 'react'
import { Typography } from '@equinor/eds-core-react'
import { FullScreenVideoStreamCard } from './FullScreenVideo'
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
    width: 90%;
    transform: translate(-50%, -50%);
    padding-top: 5rem;
    display: flex;
    justify-content: center;
    align-items: center;
`

interface VideoStreamWindowProps {
    videoStreams: MediaStreamTrack[]
}

export const VideoStreamWindow = ({ videoStreams }: VideoStreamWindowProps) => {
    const { TranslateText } = useLanguageContext()
    const [fullScreenMode, setFullScreenMode] = useState<boolean>(false)
    const [fullScreenStream, setFullScreenStream] = useState<MediaStream>()

    const toggleFullScreenMode = () => {
        setFullScreenMode(!fullScreenMode)
    }
    const updateFullScreenStream = (videoStream: MediaStream) => {
        setFullScreenStream(videoStream)
        toggleFullScreenMode()
    }

    const videoCards = videoStreams.map((videoStream, index) => (
        <VideoStreamCard
            key={index}
            videoStream={new MediaStream([videoStream])}
            videoStreamName={undefined}
            toggleFullScreenMode={toggleFullScreenMode}
            setFullScreenStream={updateFullScreenStream}
        />
    ))

    const videoStream = fullScreenStream
    return (
        <>
            <Typography variant="h2">{TranslateText('Camera')}</Typography>
            <VideoStreamContent>
                {fullScreenMode === false && videoCards}
                {videoStream && (
                    <VideoFullScreen isOpen={fullScreenMode} onRequestClose={toggleFullScreenMode}>
                        <FullScreenVideoStreamCard
                            videoStream={videoStream}
                            videoStreamName={undefined}
                            toggleFullScreenMode={toggleFullScreenMode}
                        />
                    </VideoFullScreen>
                )}
            </VideoStreamContent>
        </>
    )
}
