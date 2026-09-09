import { VideoStreamWindow } from 'pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { useContext, useEffect, useState } from 'react'
import styled from 'styled-components'
import { MissionHeader, SimpleMissionHeader } from './MissionHeader/MissionHeader'
import { Header } from 'components/Header/Header'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { AlertType, AlertKind } from 'models/Alert'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMediaStreamContext } from 'components/Contexts/MediaStreamContext'
import { StyledCardsWidth, VideoStreamSection } from 'components/Styles/StyledComponents'
import { InspectionTaskDialogView } from '../InspectionReportPage/InspectionView'
import { AnalysisOverviewSection, InspectionOverviewSection } from '../InspectionReportPage/ImageOverview'
import { TaskTableAndMap } from './TaskTableAndMap'
import { AnalysisResultDialogView } from './AnalysisResultView'
import { tokens } from '@equinor/eds-tokens'
import { useNavigate, useSearchParams } from 'react-router'
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

// lookupInspectionId is only set on the mission-simple route, where the mission is
// identified by an inspection and this hook writes the resolved id back into the URL.
// On /mission/:missionId it is undefined: there the inspection id merely selects which
// dialog is open, and must not send us looking for a different mission.
const useMissionSelector = (missionId: string | undefined, lookupInspectionId: string | undefined) => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()
    const navigate = useNavigate()
    const { TranslateText } = useLanguageContext()
    const { raiseAlert } = useAlertContext()
    const [selectedMission, setSelectedMission] = useState<Mission>()
    const { registerEvent, connectionReady } = useSignalRContext()
    const { mediaStreams, addMediaStreamConfigIfItDoesNotExist } = useMediaStreamContext()
    const backendApi = useBackendApi()

    useEffect(() => {
        if (selectedMission && !Object.keys(mediaStreams).includes(selectedMission?.robot.id))
            addMediaStreamConfigIfItDoesNotExist(selectedMission?.robot.id)
    }, [selectedMission])

    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
            const updatedMission: Mission = JSON.parse(message)
            setSelectedMission((oldMission) => (updatedMission.id === oldMission?.id ? updatedMission : oldMission))
        })
    }, [registerEvent, connectionReady])

    const videoMediaStreams = (selectedMission ? mediaStreams[selectedMission.robot.id]?.streams : undefined) ?? []

    useEffect(() => {
        if (!lookupInspectionId) return
        backendApi
            .getMissionRunByTaskId(lookupInspectionId)
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
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [lookupInspectionId, backendApi])

    useEffect(() => {
        // The id in the URL is written by the effect above once the inspection resolves,
        // so refetching it here would only duplicate that request with a mission id that
        // is stale whenever lookupInspectionId has just changed.
        if (lookupInspectionId) return

        if (!missionId) {
            navigate(`/not-found`)
            return
        }

        backendApi
            .getMissionRunById(missionId)
            .then((mission) => {
                setSelectedMission(mission)
            })
            .catch(() => {
                raiseAlert(AlertType.RequestFail, {
                    kind: AlertKind.RequestFail,
                    message: TranslateText('Failed to find mission with ID {0}', [missionId]),
                })
            })
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [missionId, lookupInspectionId, backendApi])

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
        mission.tasks.map((t) => t.id),
        null,
        null,
        null,
        null,
        null
    )

    const taskDataInSelectedMission = mission.tasks.map((t) => ({
        task: t,
        data: data?.find((d) => d.inspectionId === t.id),
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
    lookupInspectionId,
    includeHeader = true,
}: {
    missionId: string | undefined
    inspectionId: string | undefined
    analysisId: string | undefined
    /** Set by the mission-simple route only; see useMissionSelector. */
    lookupInspectionId: string | undefined
    includeHeader: boolean
}) => {
    const { selectedMission, videoMediaStreams } = useMissionSelector(missionId, lookupInspectionId)
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
