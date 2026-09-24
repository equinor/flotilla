import { VideoStreamWindow } from 'pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { useContext, useEffect, useState } from 'react'
import { MissionHeader, SimpleMissionHeader } from './MissionHeader/MissionHeader'
import { Header } from 'components/Header/Header'
import { SignalREventLabels, useSignalRContext } from 'contexts/SignalRContext'
import { useAlertContext } from 'contexts/AlertContext'
import { useLanguageContext } from 'contexts/LanguageContext'
import { PageContent, PageBackground, VideoStreamSection } from 'components/Styles/StyledComponents'
import { TaskTableAndMap } from './TaskTableAndMap'
import { useNavigate, useSearchParams } from 'react-router'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'contexts/InstallationContext'
import { useInspectionsContext } from 'contexts/InspectionsContext'
import { MissionResults } from './MissionResults/MissionResults'

// lookupInspectionId is only set on the mission-simple route, where the mission is
// identified by an inspection and this hook writes the resolved id back into the URL.
// On /mission/:missionId it is undefined: there the inspection id merely selects which
// dialog is open, and must not send us looking for a different mission.
const useMissionSelector = (missionId: string | undefined, lookupInspectionId: string | undefined) => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()
    const navigate = useNavigate()
    const { TranslateText } = useLanguageContext()
    const { setBanner } = useAlertContext()
    const [selectedMission, setSelectedMission] = useState<Mission>()
    const { registerEvent, connectionReady } = useSignalRContext()
    const backendApi = useBackendApi()

    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
            const updatedMission: Mission = JSON.parse(message)
            setSelectedMission((oldMission) => (updatedMission.id === oldMission?.id ? updatedMission : oldMission))
        })
    }, [registerEvent, connectionReady])

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
        if (selectedMission?.id === missionId) return

        backendApi
            .getMissionRunById(missionId)
            .then((mission) => {
                setSelectedMission(mission)
            })
            .catch(() => {
                const errorMessage = TranslateText('Failed to find mission with ID {0}', [missionId])
                setBanner(errorMessage, 'error')
            })
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [missionId, lookupInspectionId, backendApi])

    return { selectedMission }
}

const MissionPageWithMission = ({ mission, includeHeader = true }: { mission: Mission; includeHeader: boolean }) => {
    const { installation } = useContext(InstallationContext)
    const { useSaraListData } = useInspectionsContext()
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
            {includeHeader ? <Header installation={installation} /> : <></>}
            <PageBackground>
                <PageContent>
                    {includeHeader ? <MissionHeader mission={mission} /> : <SimpleMissionHeader mission={mission} />}
                    <TaskTableAndMap
                        tasksAndData={taskDataInSelectedMission}
                        plantCode={mission.inspectionArea.plantCode}
                        robot={mission.robot}
                    />
                    <VideoStreamSection>
                        <VideoStreamWindow robotId={mission.robot.id} />
                    </VideoStreamSection>
                    <MissionResults
                        tasks={mission.tasks}
                        data={data}
                        isPending={isPending}
                        isError={isError}
                        installationName={installation.name}
                        robotName={mission.robot.name}
                    />
                </PageContent>
            </PageBackground>
        </>
    )
}

export const MissionPage = ({
    missionId,
    lookupInspectionId,
    includeHeader = true,
}: {
    missionId: string | undefined
    /** Set by the mission-simple route only; see useMissionSelector. */
    lookupInspectionId: string | undefined
    includeHeader: boolean
}) => {
    const { selectedMission } = useMissionSelector(missionId, lookupInspectionId)
    const { installation } = useContext(InstallationContext)

    return selectedMission ? (
        <MissionPageWithMission mission={selectedMission} includeHeader={includeHeader} />
    ) : (
        <>
            {includeHeader ? <Header installation={installation} /> : <></>}
            <PageBackground />
        </>
    )
}
