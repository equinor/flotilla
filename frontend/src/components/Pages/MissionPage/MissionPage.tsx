import { TaskTable } from 'components/Pages/MissionPage/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/Pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { VideoStream } from 'models/VideoStream'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { MissionHeader } from './MissionHeader/MissionHeader'
import { BackButton } from 'utils/BackButton'
import { MissionMapView } from './MapPosition/MissionMapView'
import { BackendAPICaller } from 'api/ApiCaller'
import { Header } from 'components/Header/Header'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

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

export const MissionPage = () => {
    const { missionId } = useParams()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [videoStreams, setVideoStreams] = useState<VideoStream[]>([])
    const [selectedMission, setSelectedMission] = useState<Mission>()
    const { registerEvent, connectionReady } = useSignalRContext()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
                let updatedMission: Mission = JSON.parse(message)
                setSelectedMission((oldMission) => (updatedMission.id === oldMission?.id ? updatedMission : oldMission))
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [connectionReady])

    useEffect(() => {
        const updateVideoStreams = (mission: Mission) =>
            BackendAPICaller.getVideoStreamsByRobotId(mission.robot.id)
                .then((streams) => setVideoStreams(streams))
                .catch((e) => {
                    console.warn(`Failed to get video stream with robot ID ${mission.robot.id}`)
                })

        if (missionId)
            BackendAPICaller.getMissionRunById(missionId)
                .then((mission) => {
                    setSelectedMission(mission)
                    updateVideoStreams(mission)
                })
                .catch((e) => {
                    setAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertContent
                            translatedMessage={TranslateText('Failed to find mission with ID {0}', [missionId])}
                        />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertListContent
                            translatedMessage={TranslateText('Failed to find mission with ID {0}', [missionId])}
                        />,
                        AlertCategory.ERROR
                    )
                })
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [missionId])

    return (
        <>
            <Header page={'mission'} />
            <StyledMissionPage>
                <BackButton />
                {selectedMission !== undefined && (
                    <>
                        <MissionHeader mission={selectedMission} />
                        <TaskAndMapSection>
                            <TaskTable tasks={selectedMission?.tasks} />
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
