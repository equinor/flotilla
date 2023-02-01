import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

import { VideoStream } from 'models/VideoStream'
import { VideoPlayerOvenPlayer, IsValidOvenPlayerType } from './VideoPlayerOvenPlayer'
import { VideoPlayerSimple } from './VideoPlayerSimple'

const VideoCard = styled(Card)`
    padding: 16px;
    height: 15rem;
    width: 20rem;
`

const StyledVideoSection = styled(Card)`
    height: 95%;
    display: inline-flex;
`
const VideoStreamContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 3rem;
    padding-top: 16px;
    padding-bottom: 16px;
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
        videoPlayer = <VideoPlayerOvenPlayer videoStream={videoStream} />
    } else {
        videoPlayer = <VideoPlayerSimple videoStream={videoStream} />
    }

    return (
        <VideoCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <StyledVideoSection>{videoPlayer}</StyledVideoSection>
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
