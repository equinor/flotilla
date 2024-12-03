import { TaskTable } from 'components/Pages/MissionPage/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/Pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
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
import { useMediaStreamContext } from 'components/Contexts/MediaStreamContext'
import { tokens } from '@equinor/eds-tokens'
import { StyledPage } from 'components/Styles/StyledComponents'
import { Task } from 'models/Task'
import { InspectionDialogView, InspectionsViewSection } from './InpectionView/InspectionView'

const StyledMissionPage = styled(StyledPage)`
    background-color: ${tokens.colors.ui.background__light.hex};
`
const TaskAndMapSection = styled.div`
    display: flex;
    align-items: flex-start;
    flex-wrap: wrap;
    gap: 8rem;
    padding-top: 16px;
    padding-bottom: 16px;
`
export const VideoStreamSection = styled.div`
    display: grid;
    gap: 1rem;
`

export const MissionPage = () => {
    const { missionId } = useParams()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [videoMediaStreams, setVideoMediaStreams] = useState<MediaStreamTrack[]>([])
    const [selectedMission, setSelectedMission] = useState<Mission>()
    const { registerEvent, connectionReady } = useSignalRContext()
    const { mediaStreams, addMediaStreamConfigIfItDoesNotExist } = useMediaStreamContext()
    const [inspectionTask, setInspectionTask] = useState<Task>()

    useEffect(() => {
        if (selectedMission && !Object.keys(mediaStreams).includes(selectedMission?.robot.id))
            addMediaStreamConfigIfItDoesNotExist(selectedMission?.robot.id)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedMission])

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
        if (selectedMission && mediaStreams && Object.keys(mediaStreams).includes(selectedMission?.robot.id)) {
            const mediaStreamConfig = mediaStreams[selectedMission?.robot.id]
            if (mediaStreamConfig && mediaStreamConfig.streams.length > 0)
                setVideoMediaStreams(mediaStreamConfig.streams)
        }
    }, [selectedMission, mediaStreams])

    useEffect(() => {
        if (missionId)
            BackendAPICaller.getMissionRunById(missionId)
                .then((mission) => {
                    setSelectedMission(mission)
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
                            <TaskTable tasks={selectedMission?.tasks} setInspectionTask={setInspectionTask} />
                            <MissionMapView mission={selectedMission} />
                        </TaskAndMapSection>
                        <VideoStreamSection>
                            {videoMediaStreams && videoMediaStreams.length > 0 && (
                                <VideoStreamWindow videoStreams={videoMediaStreams} />
                            )}
                        </VideoStreamSection>
                        {inspectionTask && inspectionTask.isarTaskId && (
                            <InspectionDialogView
                                task={inspectionTask}
                                setInspectionTask={setInspectionTask}
                                tasks={selectedMission.tasks}
                            />
                        )}
                        <InspectionsViewSection tasks={selectedMission.tasks} setInspectionTask={setInspectionTask} />
                    </>
                )}
            </StyledMissionPage>
        </>
    )
}
