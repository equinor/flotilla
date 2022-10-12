import { Button } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { TaskTable } from 'components/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
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
    display: grid;
    gap: 1rem;
`

export function MissionPage() {
    const { missionId } = useParams()
    const apiCaller = useApi()
    const [videoStreams, setVideoStreams] = useState<VideoStream[]>([])
    const [selectedMission, setSelectedMission] = useState<Mission>()

    useEffect(() => {
        if (missionId) {
            apiCaller.getMissionById(missionId).then((mission) => {
                setSelectedMission(mission)
                updateVideoStreams(mission)
            })
        }
    }, [])

    useEffect(() => {
        const timeDelay = 1000
        const id = setInterval(() => {
            if (missionId) {
                apiCaller.getMissionById(missionId).then((mission) => {
                    setSelectedMission(mission)
                })
            }
        }, timeDelay)
        return () => clearInterval(id)
    }, [])

    const updateVideoStreams = (mission: Mission) => {
        apiCaller.getVideoStreamsByRobotId(mission.robot.id).then((streams) => {
            setVideoStreams(streams)
        })
    }

    var videoDisplay = videoStreams.map(function (videoStream, index) {
        return <VideoStreamWindow key={index} videoStream={videoStream} />
    })

    return (
        <StyledMissionPage>
            <VideoStreamSection>{videoStreams.length > 0 && videoDisplay}</VideoStreamSection>
            <TaskTable mission={selectedMission} />
            <Button href="..">FrontPage</Button>
        </StyledMissionPage>
    )
}
