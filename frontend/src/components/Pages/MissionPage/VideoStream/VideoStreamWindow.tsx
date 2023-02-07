import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { VideoStream } from 'models/VideoStream'
import { VideoPlayerOvenPlayer, IsValidOvenPlayerType } from './VideoPlayerOvenPlayer'
import { VideoPlayerSimple } from './VideoPlayerSimple'

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

const StyledVideoSectionRotated = styled.div`
    width: 10rem;
    height: 10rem;
`

const VideoStreamContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 3rem;
    padding-top: 1rem;
    padding-bottom: 1rem;
`

interface VideoStreamWindowProps {
    videoStreams: VideoStream[]
}

interface VideoStreamCardProps {
    videoStream: VideoStream
}

function VideoStreamCard({ videoStream }: VideoStreamCardProps) {
    var videoPlayer = null
    if (IsValidOvenPlayerType({ videoStream })) {
        if (videoStream.shouldRotate) {
            videoPlayer = (
                <StyledVideoSectionRotated>
                    <Rotate>
                        <VideoPlayerOvenPlayer videoStream={videoStream} />
                    </Rotate>
                </StyledVideoSectionRotated>
            )
        } else {
            videoPlayer = (
                <StyledVideoSection>
                    <VideoPlayerOvenPlayer videoStream={videoStream} />
                </StyledVideoSection>
            )
        }
    } else {
        videoPlayer = (
            <StyledVideoSection>
                <VideoPlayerSimple videoStream={videoStream} />
            </StyledVideoSection>
        )
    }

    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            {videoPlayer}
            <Typography variant="h5">{videoStream.name}</Typography>
        </VideoCard>
    )
}

export function VideoStreamWindow({ videoStreams }: VideoStreamWindowProps) {
    var videoCards = videoStreams.map(function (videoStream, index) {
        return <VideoStreamCard key={index} videoStream={videoStream} />
    })

    return (
        <>
            <Typography variant="h2">Camera</Typography>
            <VideoStreamContent>{videoCards}</VideoStreamContent>
        </>
    )
}
