import { TaskTable } from 'components/Pages/MissionPage/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/Pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { VideoStream } from 'models/VideoStream'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { MissionHeader } from './MissionHeader/MissionHeader'
import { BackButton } from './MissionHeader/BackButton'
import { MapView } from './MapPosition/MapView'
import { useErrorHandler } from 'react-error-boundary'
import { BackendAPICaller } from 'api/ApiCaller'

const StyledMissionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
`
const TaskAndMapSection = styled.div`
    display: flex;
    align-items: flex-start;
    flex-wrap: wrap;
    gap: 8rem;
    padding-top: 16px;
    padding-bottom: 16px;
`
const VideoStreamSection = styled.div`
    display: grid;
    gap: 1rem;
`

export function MissionPage() {
    const { missionId } = useParams()
    const handleError = useErrorHandler()
    const [videoStreams, setVideoStreams] = useState<VideoStream[]>([])
    const [selectedMission, setSelectedMission] = useState<Mission>()

    useEffect(() => {
        if (missionId) {
            BackendAPICaller.getMissionById(missionId).then((mission) => {
                setSelectedMission(mission)
                updateVideoStreams(mission)
            })
            //.catch((e) => handleError(e))
        }
    }, [])

    useEffect(() => {
        const timeDelay = 1000
        const id = setInterval(() => {
            if (missionId) {
                BackendAPICaller.getMissionById(missionId).then((mission) => {
                    setSelectedMission(mission)
                })
                //.catch((e) => handleError(e))
            }
        }, timeDelay)
        return () => clearInterval(id)
    }, [])

    const updateVideoStreams = (mission: Mission) => {
        BackendAPICaller.getVideoStreamsByRobotId(mission.robot.id).then((streams) => {
            setVideoStreams(streams)
        })
        //.catch((e) => handleError(e))
    }

    return (
        <StyledMissionPage>
            <BackButton />
            {selectedMission !== undefined && (
                <>
                    <MissionHeader mission={selectedMission} />
                    <TaskAndMapSection>
                        <TaskTable mission={selectedMission} />
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
