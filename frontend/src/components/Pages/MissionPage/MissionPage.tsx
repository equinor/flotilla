import { Typography } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { TaskTable } from 'components/Pages/MissionPage/TaskOverview/TaskTable'
import { MapPositionView } from 'components/Pages/MissionPage/MapPosition/MapPositionView'
import { VideoStreamWindow } from 'components/Pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { VideoStream } from 'models/VideoStream'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { MissionHeader } from './MissionHeader/MissionHeader'
import { BackButton } from './MissionHeader/BackButton'
import { MapView } from './MapPosition/MapView'

const StyledMissionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
`
const TaskAndMapSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 3rem;
    padding-top: 16px;
    padding-bottom: 16px;
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

    return (
        <StyledMissionPage>
            <BackButton />
            {selectedMission !== undefined && (
                <>
                    <MissionHeader mission={selectedMission} />
                    <TaskAndMapSection>
                        <TaskTable mission={selectedMission} />
                        <MapPositionView mission={selectedMission} />
                        <MapView mission={selectedMission} />
                    </TaskAndMapSection>
                    <VideoStreamSection>
                        {videoStreams.length > 0 && <VideoStreamWindow videoStreams={videoStreams} />}
                    </VideoStreamSection>
                </>
            )}
        </StyledMissionPage>
    )
}
