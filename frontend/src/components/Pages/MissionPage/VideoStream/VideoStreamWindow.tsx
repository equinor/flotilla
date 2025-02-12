import { Typography } from '@equinor/eds-core-react'
import { VideoStreamCard } from './VideoStreamCards'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

const VideoStreamContent = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 3rem;
    padding-top: 1rem;
    padding-bottom: 1rem;
`

interface VideoStreamWindowProps {
    videoStreams: MediaStreamTrack[]
}

export const VideoStreamWindow = ({ videoStreams }: VideoStreamWindowProps) => {
    const { TranslateText } = useLanguageContext()

    const videoCards = videoStreams.map((videoStream, index) => (
        <VideoStreamCard
            key={index}
            videoStream={new MediaStream([videoStream])}
            videoStreamName={undefined}
            videoStreamId={'videostreamid-' + index}
        />
    ))

    return (
        <>
            <Typography variant="h2">{TranslateText('Camera')}</Typography>
            <VideoStreamContent>{videoCards}</VideoStreamContent>
        </>
    )
}
