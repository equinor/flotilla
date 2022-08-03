import { Button } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { TaskTable } from 'components/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/VideoStream/VideoStreamWindow'
import { VideoStream } from 'models/VideoStream'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'

const StyledMissionPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 3rem;
`
const VideoStreamSection = styled.div`
    display: flex;
`

export function MissionPage() {
    const { missionId } = useParams()
    const apiCaller = useApi()
    const [videoStreams, setVideoStreams] = useState<VideoStream[]>([])
    useEffect(() => {
        if (missionId) {
            apiCaller.getMissionById(missionId).then((mission) => {
                apiCaller.getVideoStreamsByRobotId(mission.robot.id).then((streams) => {
                    setVideoStreams(streams)
                })
            })
        }
    }, [])

    var videoDisplay = videoStreams.map(function (videoStream, index) {
        return <VideoStreamWindow key={index} videoStream={videoStream} />
    })
    return (
        <StyledMissionPage>
            <VideoStreamSection>
                {videoStreams.length > 0 && videoDisplay}
            </VideoStreamSection>
            <TaskTable />
            <Button href="..">FrontPage</Button>
        </StyledMissionPage>
    )
}
