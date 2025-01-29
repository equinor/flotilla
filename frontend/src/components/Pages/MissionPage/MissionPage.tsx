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
import { InspectionDialogView, InspectionsViewSection } from '../InspectionReportPage/InspectionView'
import { useInspectionsContext } from 'components/Contexts/InpectionsContext'
import { Typography } from '@equinor/eds-core-react'

const StyledMissionPage = styled(StyledPage)`
    background-color: ${tokens.colors.ui.background__light.hex};
`
const TaskAndMapSection = styled.div`
    display: flex;
    min-height: 60%;
    padding: 24px;
    @media (max-width: 600px) {
        padding: 6px 8px 8px 6px;
    }
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
    gap: 8px;
    align-self: stretch;
    border-radius: 6px;
    border: 1px solid ${tokens.colors.ui.background__medium.rgba};
    background: ${tokens.colors.ui.background__default.rgba};
`

const StyledTableAndMap = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: top;
    gap: 30px;
`
const StyledMissionPageContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 10px;
`

const StyledCardsWidth = styled.div`
    display: flex;
    flex-direction: column;
    min-width: 50%;
    max-width: fit-content;
    gap: 20px;
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
            <StyledMissionPage>
                <BackButton />
                {selectedMission !== undefined && (
                    <StyledMissionPageContent>
                        <StyledCardsWidth>
                            <MissionHeader mission={selectedMission} />
                            <TaskAndMapSection>
                                <Typography variant="h4">{TranslateText('Tasks')}</Typography>
                                <StyledTableAndMap>
                                    <TaskTable tasks={selectedMission?.tasks} />
                                    {selectedMission.missionId && <MissionMapView mission={selectedMission} />}
                                </StyledTableAndMap>
                            </TaskAndMapSection>
                        </StyledCardsWidth>
                        <VideoStreamSection>
                            {videoMediaStreams && videoMediaStreams.length > 0 && (
                                <VideoStreamWindow videoStreams={videoMediaStreams} />
                            )}
                        </VideoStreamSection>
                        {selectedInspectionTask && selectedInspectionTask.isarTaskId && (
                            <InspectionDialogView task={selectedInspectionTask} tasks={selectedMission.tasks} />
                        )}
                        <InspectionsViewSection tasks={selectedMission.tasks} />
                    </StyledMissionPageContent>
                )}
            </StyledMissionPage>
        </>
    )
}
