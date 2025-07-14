import { VideoStreamWindow } from 'components/Pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { MissionHeader } from './MissionHeader/MissionHeader'
import { BackButton } from 'utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'
import { Header } from 'components/Header/Header'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useMediaStreamContext } from 'components/Contexts/MediaStreamContext'
import { StyledPage } from 'components/Styles/StyledComponents'
import { InspectionDialogView } from '../InspectionReportPage/InspectionView'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { InspectionOverviewSection } from '../InspectionReportPage/InspectionOverview'
import { TaskTableAndMap } from './TaskTableAndMap'

const StyledMissionPageContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 10px;
`

const StyledCardsWidth = styled.div`
    display: flex;
    flex-direction: column;
    max-width: fit-content;
    gap: 20px;
`

export const VideoStreamSection = styled.div`
    display: grid;
    gap: 1rem;
`

export const MissionPage = ({ missionId }: { missionId: string }) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [videoMediaStreams, setVideoMediaStreams] = useState<MediaStreamTrack[]>([])
    const [selectedMission, setSelectedMission] = useState<Mission>()
    const { registerEvent, connectionReady } = useSignalRContext()
    const { mediaStreams, addMediaStreamConfigIfItDoesNotExist } = useMediaStreamContext()
    const { selectedInspectionTask } = useInspectionsContext()

    useEffect(() => {
        if (selectedMission && !Object.keys(mediaStreams).includes(selectedMission?.robot.id))
            addMediaStreamConfigIfItDoesNotExist(selectedMission?.robot.id)
    }, [selectedMission])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
                const updatedMission: Mission = JSON.parse(message)
                setSelectedMission((oldMission) => (updatedMission.id === oldMission?.id ? updatedMission : oldMission))
            })
        }
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
                .catch(() => {
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
    }, [missionId])

    return (
        <>
            <Header page={'mission'} />
            <StyledPage>
                <BackButton />
                {selectedMission !== undefined && (
                    <StyledMissionPageContent>
                        <StyledCardsWidth>
                            <MissionHeader mission={selectedMission} />
                            <TaskTableAndMap mission={selectedMission} missionDefinitionPage={false} />
                            <VideoStreamSection>
                                {videoMediaStreams && videoMediaStreams.length > 0 && (
                                    <VideoStreamWindow videoStreams={videoMediaStreams} />
                                )}
                            </VideoStreamSection>
                            {selectedInspectionTask && selectedInspectionTask.id && (
                                <InspectionDialogView
                                    selectedTask={selectedInspectionTask}
                                    tasks={selectedMission.tasks}
                                />
                            )}
                            <InspectionOverviewSection tasks={selectedMission.tasks} />
                        </StyledCardsWidth>
                    </StyledMissionPageContent>
                )}
            </StyledPage>
        </>
    )
}
