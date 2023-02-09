import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { VideoStream } from 'models/VideoStream'
import { VideoPlayerOvenPlayer, IsValidOvenPlayerType } from './VideoPlayerOvenPlayer'
import { VideoPlayerSimple } from './VideoPlayerSimple'
import { FullScreenVideoStreamCard } from './FullScreenVideo'
import { useState } from 'react'
import ReactModal from 'react-modal'

const VideoCard = styled(Card)`
    padding: 1rem;
    height: 15rem;
    width: 20rem;
`

const StyledVideoSection = styled.div`
    height: 10rem;
`
const Rotate = styled.div`
    transform: rotate(270deg);
    position: relative;
    left: 4rem;
    bottom: 4rem;
`

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

interface VideoStreamCardProps {
    videoStream: VideoStream
    toggleFullScreenMode: VoidFunction
    setFullScreenStream: Function
}

function VideoStreamCard({ videoStream, toggleFullScreenMode, setFullScreenStream }: VideoStreamCardProps) {
    var videoPlayer = null

    const turnOnFullScreen = () => {
        setFullScreenStream(videoStream)
        toggleFullScreenMode()
    }

    if (IsValidOvenPlayerType({ videoStream })) {
        if (videoStream.shouldRotate) {
            videoPlayer = (
                <StyledVideoSection style={{ width: '10rem' }}>
                    <Rotate>
                        <VideoPlayerOvenPlayer videoStream={videoStream} />
                    </Rotate>
                </StyledVideoSection>
            )
        } else {
            videoPlayer = (
                <StyledVideoSection>
                    <VideoPlayerOvenPlayer videoStream={videoStream} />
                </StyledVideoSection>
            )
        }
    } else {
        // Rotated stream is not supported for simpleplayer
        videoPlayer = (
            <StyledVideoSection>
                <VideoPlayerSimple videoStream={videoStream} />
            </StyledVideoSection>
        )
    }

    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <div onDoubleClick={turnOnFullScreen}>{videoPlayer}</div>
            <Typography variant="h5">{videoStream.name}</Typography>
        </VideoCard>
    )
}

export function VideoStreamWindow({ videoStreams }: VideoStreamWindowProps) {
    const [fullScreenMode, setFullScreenMode] = useState<boolean>(false)
    const [fullScreenStream, setFullScreenStream] = useState<VideoStream>()

    const toggleFullScreenMode = () => {
        setFullScreenMode(!fullScreenMode)
    }
    const updateFullScreenStream = (videoStream: VideoStream) => {
        setFullScreenStream(videoStream)
        toggleFullScreenMode()
    }

    var videoCards = videoStreams.map(function (videoStream, index) {
        return (
            <VideoStreamCard
                key={index}
                videoStream={videoStream}
                toggleFullScreenMode={toggleFullScreenMode}
                setFullScreenStream={updateFullScreenStream}
            />
        )
    })

    return (
        <>
            <Typography variant="h2">Camera</Typography>
            <VideoStreamContent>
                {fullScreenMode === false && videoCards}
                {fullScreenStream && (
                    <VideoFullScreen isOpen={fullScreenMode} onRequestClose={toggleFullScreenMode}>
                        {FullScreenVideoStreamCard(fullScreenStream)}
                    </VideoFullScreen>
                )}
            </VideoStreamContent>
        </>
    )
}
