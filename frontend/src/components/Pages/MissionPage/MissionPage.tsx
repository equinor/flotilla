import { TaskTable } from 'components/Pages/MissionPage/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/Pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { VideoStream } from 'models/VideoStream'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { MissionHeader } from './MissionHeader/MissionHeader'
import { BackButton } from './MissionHeader/BackButton'
import { MissionMapView } from './MapPosition/MissionMapView'
import { BackendAPICaller } from 'api/ApiCaller'

import { Header } from 'components/Header/Header'

const StyledMissionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
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
    const [videoStreams, setVideoStreams] = useState<VideoStream[]>([])
    const [selectedMission, setSelectedMission] = useState<Mission>()

    useEffect(() => {
        if (missionId) {
            BackendAPICaller.getMissionRunById(missionId).then((mission) => {
                setSelectedMission(mission)
                updateVideoStreams(mission)
            })
        }
    }, [missionId])

    useEffect(() => {
        const timeDelay = 1000
        const id = setInterval(() => {
            if (missionId) {
                BackendAPICaller.getMissionRunById(missionId).then((mission) => {
                    setSelectedMission(mission)
                })
            }
        }, timeDelay)
        return () => clearInterval(id)
    }, [missionId])

    const updateVideoStreams = (mission: Mission) => {
        BackendAPICaller.getVideoStreamsByRobotId(mission.robot.id).then((streams) => {
            setVideoStreams(streams)
        })
    }

    return (
        <>
            <Header page={'mission'} />
            <StyledMissionPage>
                <BackButton />
                {selectedMission !== undefined && (
                    <>
                        <MissionHeader mission={selectedMission} />
                        <TaskAndMapSection>
                            <TaskTable mission={selectedMission} />
                            <MissionMapView mission={selectedMission} />
                        </TaskAndMapSection>
                        <VideoStreamSection>
                            {videoStreams.length > 0 && <VideoStreamWindow videoStreams={videoStreams} />}
                        </VideoStreamSection>
                    </>
                )}
            </StyledMissionPage>
        </>
    )
}
