import { VideoStreamWindow } from 'pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { useContext, useEffect, useLayoutEffect, useRef, useState } from 'react'
import styled from 'styled-components'
import { MissionHeader, SimpleMissionHeader } from './MissionHeader/MissionHeader'
import { Header } from 'components/Header/Header'
import { SignalREventLabels, useSignalRContext } from 'contexts/SignalRContext'
import { useAlertContext } from 'contexts/AlertContext'
import { useLanguageContext } from 'contexts/LanguageContext'
import { StyledCardsWidth, VideoStreamSection } from 'components/Styles/StyledComponents'
import { TaskTableAndMap } from './TaskTableAndMap'
import { tokens } from '@equinor/eds-tokens'
import { useNavigate, useSearchParams } from 'react-router'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'contexts/InstallationContext'
import { useInspectionsContext } from 'contexts/InspectionsContext'
import { MissionResults } from './MissionResults/MissionResults'

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
    const body = useRef<HTMLDivElement>(null)
    const results = useRef<HTMLDivElement>(null)
    const [resultsWidth, setResultsWidth] = useState<number>()

    useLayoutEffect(() => {
        const content = body.current
        const resultSection = results.current
        const map = content?.querySelector('.map-root')
        const table = content?.querySelector('table')
        const boundary = map ?? table
        if (!content || !resultSection || !boundary) {
            setResultsWidth(undefined)
            return
        }
        const updateWidth = () => {
            const isStacked = map && table && map.getBoundingClientRect().top >= table.getBoundingClientRect().bottom
            setResultsWidth(
                isStacked
                    ? undefined
                    : boundary.getBoundingClientRect().right - resultSection.getBoundingClientRect().left
            )
        }
        const observer = new ResizeObserver(updateWidth)
        observer.observe(content)
        observer.observe(boundary)
        if (table && table !== boundary) observer.observe(table)
        updateWidth()
        return () => observer.disconnect()
    }, [mission.inspectionArea.plantCode])

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
            <StyledMissionPage>
                <StyledMissionPageContent>
                    {includeHeader ? <MissionHeader mission={mission} /> : <SimpleMissionHeader mission={mission} />}
                    <StyledMissionPageBody ref={body}>
                        <StyledCardsWidth>
                            <TaskTableAndMap
                                tasksAndData={taskDataInSelectedMission}
                                plantCode={mission.inspectionArea.plantCode}
                                robot={mission.robot}
                            />
                            <VideoStreamSection>
                                <VideoStreamWindow robotId={mission.robot.id} />
                            </VideoStreamSection>
                        </StyledCardsWidth>
                        <div ref={results} style={{ maxWidth: resultsWidth, minWidth: 0 }}>
                            <MissionResults
                                tasks={mission.tasks}
                                data={data}
                                isPending={isPending}
                                isError={isError}
                                installationName={installation.name}
                                robotName={mission.robot.name}
                            />
                        </div>
                    </StyledMissionPageBody>
                </StyledMissionPageContent>
            </StyledMissionPage>
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
            <StyledMissionPage />
        </>
    )
}
