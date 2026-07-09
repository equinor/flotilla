import { VideoStreamWindow } from 'pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { useContext, useEffect, useState } from 'react'
import styled from 'styled-components'
import { MissionHeader, SimpleMissionHeader } from './MissionHeader/MissionHeader'
import { Header } from 'components/Header/Header'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useMediaStreamContext } from 'components/Contexts/MediaStreamContext'
import { StyledCardsWidth, VideoStreamSection } from 'components/Styles/StyledComponents'
import { InspectionTaskDialogView } from '../InspectionReportPage/InspectionView'
import { AnalysisOverviewSection, InspectionOverviewSection } from '../InspectionReportPage/ImageOverview'
import { TaskTableAndMap } from './TaskTableAndMap'
import { AnalysisResultDialogView } from './AnalysisResultView'
import { tokens } from '@equinor/eds-tokens'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { PendingResultPlaceholder, TextAsImage } from 'pages/InspectionReportPage/InspectionReportImage'

const StyledMissionPageContent = styled.div`
    display: flex;
    flex-direction: column;
`

const StyledMissionPage = styled.div`
    display: flex;
    flex-direction: column;
    background: ${tokens.colors.ui.background__default.hex};
    min-height: 100vh;
`

const StyledMissionPageBody = styled.div`
    padding: 1.5rem 4rem 2rem 4rem;
    display: flex;
    flex-direction: column;
    gap: 2rem;
    @media (max-width: 960px) {
        padding: 1rem 1.5rem 1.5rem 1.5rem;
    }
`

const useMissionSelector = (missionId: string | undefined, inspectionId: string | undefined) => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()
    const navigate = useNavigate()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [selectedMission, setSelectedMission] = useState<Mission>()
    const { registerEvent, connectionReady } = useSignalRContext()
    const { mediaStreams, addMediaStreamConfigIfItDoesNotExist } = useMediaStreamContext()
    const backendApi = useBackendApi()

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

    const videoMediaStreams = (selectedMission ? mediaStreams[selectedMission.robot.id]?.streams : undefined) ?? []

    useEffect(() => {
        if (missionId)
            backendApi
                .getMissionRunById(missionId)
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
        else if (inspectionId) {
            backendApi
                .getMissionRunByIsarInspectionId(inspectionId)
                .then((mission) => {
                    setSearchParams(
                        (prev) => {
                            prev.set('id', mission.id)
                            return prev
                        },
                        { replace: true }
                    )
                    setSelectedMission(mission)
                })
                .catch(() => {
                    navigate(`/not-found`)
                })
        } else {
            navigate(`/not-found`)
        }
    }, [missionId])

    return { selectedMission, videoMediaStreams }
}

const MissionPageWithMission = ({
    mission,
    videoMediaStreams,
    inspectionId,
    analysisId,
    includeHeader = true,
}: {
    mission: Mission
    videoMediaStreams: MediaStreamTrack[]
    inspectionId: string | undefined
    analysisId: string | undefined
    includeHeader: boolean
}) => {
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)
    const { useSaraListData } = useInspectionsContext()

    const hasAnalysisType = mission.tasks.some((task) => task.analysisTypes.length > 0)

    const { data, isPending, isError } = useSaraListData(
        mission.tasks.map((t) => t.inspection.isarInspectionId),
        null,
        null,
        null,
        null,
        null
    )

    const taskDataInSelectedMission = mission.tasks.map((t) => ({
        task: t,
        data: data?.find((d) => d.inspectionId === t.inspection.isarInspectionId),
    }))

    return (
        <>
            {includeHeader ? <Header alertDict={alerts} installation={installation} /> : <></>}
            <StyledMissionPage>
                <StyledMissionPageContent>
                    {includeHeader ? <MissionHeader mission={mission} /> : <SimpleMissionHeader mission={mission} />}
                    <StyledMissionPageBody>
                        <StyledCardsWidth>
                            <TaskTableAndMap
                                tasksAndData={taskDataInSelectedMission}
                                plantCode={mission.inspectionArea.plantCode}
                                robot={mission.robot}
                            />
                            <VideoStreamSection>
                                {videoMediaStreams && videoMediaStreams.length > 0 && (
                                    <VideoStreamWindow videoStreams={videoMediaStreams} />
                                )}
                            </VideoStreamSection>
                            {inspectionId && data && (
                                <InspectionTaskDialogView selectedInspectionId={inspectionId} inspectionData={data} />
                            )}
                            {analysisId && data && (
                                <AnalysisResultDialogView selectedInspectionId={analysisId} inspectionData={data} />
                            )}
                            {!isPending && data && <InspectionOverviewSection inspectionData={data} />}
                            {!isPending && hasAnalysisType && data && <AnalysisOverviewSection inspectionData={data} />}
                            {isPending && <PendingResultPlaceholder isLargeImage={true} />}
                            {isError && <TextAsImage isLargeImage={true} text={'No inspection could be found'} />}
                        </StyledCardsWidth>
                    </StyledMissionPageBody>
                </StyledMissionPageContent>
            </StyledMissionPage>
        </>
    )
}

export const MissionPage = ({
    missionId,
    inspectionId,
    analysisId,
    includeHeader = true,
}: {
    missionId: string | undefined
    inspectionId: string | undefined
    analysisId: string | undefined
    includeHeader: boolean
}) => {
    const { selectedMission, videoMediaStreams } = useMissionSelector(missionId, undefined)
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)

    return selectedMission ? (
        <MissionPageWithMission
            mission={selectedMission}
            videoMediaStreams={videoMediaStreams}
            inspectionId={inspectionId}
            analysisId={analysisId}
            includeHeader={includeHeader}
        />
    ) : (
        <>
            {includeHeader ? <Header alertDict={alerts} installation={installation} /> : <></>}
            <StyledMissionPage />
        </>
    )
}
