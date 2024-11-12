import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { BackendAPICaller } from 'api/ApiCaller'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { TaskStatus } from 'models/Task'
import { useLanguageContext } from './LanguageContext'
import { AlertType, useAlertContext } from './AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useInstallationContext } from './InstallationContext'

const upsertMissionList = (list: Mission[], mission: Mission) => {
    let newMissionList = [...list]
    const i = newMissionList.findIndex((e) => e.id === mission.id)
    if (i > -1) newMissionList[i] = mission
    else newMissionList.push(mission)
    return newMissionList
}
interface IMissionRunsContext {
    ongoingMissions: Mission[]
    missionQueue: Mission[]
    loadingMissionSet: Set<string>
    setLoadingMissionSet: (newLoadingMissionSet: Set<string> | ((mission: Set<string>) => Set<string>)) => void
}

interface Props {
    children: React.ReactNode
}

const defaultMissionRunsContext: IMissionRunsContext = {
    ongoingMissions: [],
    missionQueue: [],
    loadingMissionSet: new Set(),
    setLoadingMissionSet: (newLoadingMissionSet: Set<string> | ((mission: Set<string>) => Set<string>)) => {},
}

const MissionRunsContext = createContext<IMissionRunsContext>(defaultMissionRunsContext)

const updateQueueIfMissionAlreadyQueued = (oldQueue: Mission[], updatedMission: Mission) => {
    const existingMissionIndex = oldQueue.findIndex((m) => m.id === updatedMission.id)
    if (existingMissionIndex !== -1) {
        // If the mission is already in the queue
        if (updatedMission.status !== MissionStatus.Pending) oldQueue.splice(existingMissionIndex, 1)
        else oldQueue[existingMissionIndex] = updatedMission
    }
    return oldQueue
}

const anyRemainingTasks = (mission: Mission) =>
    !mission.tasks.every((t) => t.status !== TaskStatus.InProgress && t.status !== TaskStatus.NotStarted)

const updateOngoingMissionsWithUpdatedMission = (oldMissionList: Mission[], updatedMission: Mission) => {
    const existingMissionIndex = oldMissionList.findIndex((m) => m.id === updatedMission.id)
    if (updatedMission.status === MissionStatus.Ongoing || updatedMission.status === MissionStatus.Paused) {
        if (existingMissionIndex !== -1) {
            // Mission is ongoing and in the queue
            oldMissionList[existingMissionIndex] = updatedMission
        } else {
            // Mission is ongoing and not in the queue
            if (anyRemainingTasks(updatedMission))
                // Do not add missions with no remaining tasks
                return [...oldMissionList, updatedMission]
        }
    } else {
        if (existingMissionIndex !== -1) {
            // Mission is not ongoing and in the queue
            oldMissionList.splice(existingMissionIndex, 1)
        }
    }
    return oldMissionList
}

const fetchMissionRuns = (params: {
    statuses: MissionStatus[]
    pageSize: number
    orderBy: string
}): Promise<Mission[]> => BackendAPICaller.getMissionRuns(params).then((response) => response.content)

const useMissionRuns = (): IMissionRunsContext => {
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [missionQueue, setMissionQueue] = useState<Mission[]>([])
    const [loadingMissionSet, setLoadingMissionSet] = useState<Set<string>>(new Set())
    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const { installationCode } = useInstallationContext()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunCreated, (username: string, message: string) => {
                const newMission: Mission = JSON.parse(message)
                setMissionQueue((oldQueue) => {
                    let missionQueueCopy = upsertMissionList(oldQueue, newMission)
                    return [...missionQueueCopy]
                })
            })
            registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
                let updatedMission: Mission = JSON.parse(message)

                setMissionQueue((oldQueue) => {
                    const oldQueueCopy = [...oldQueue]
                    return updateQueueIfMissionAlreadyQueued(oldQueueCopy, updatedMission)
                })
                setOngoingMissions((oldMissionList) => {
                    const oldMissionListCopy = [...oldMissionList]
                    return updateOngoingMissionsWithUpdatedMission(oldMissionListCopy, updatedMission)
                })
            })
            registerEvent(SignalREventLabels.missionRunDeleted, (username: string, message: string) => {
                let deletedMission: Mission = JSON.parse(message)
                setOngoingMissions((missions) => {
                    const oldMissionListCopy = [...missions]
                    const ongoingIndex = oldMissionListCopy.findIndex((m) => m.id === deletedMission.id)
                    if (ongoingIndex !== -1) oldMissionListCopy.splice(ongoingIndex, 1) // Remove deleted mission
                    return oldMissionListCopy
                })
                setMissionQueue((missions) => {
                    const oldQueueCopy = [...missions]
                    const queueIndex = oldQueueCopy.findIndex((m) => m.id === deletedMission.id)
                    if (queueIndex !== -1) oldQueueCopy.splice(queueIndex, 1) // Remove deleted mission
                    return oldQueueCopy
                })
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [registerEvent, connectionReady])

    useEffect(() => {
        const fetchAndUpdateMissions = async () => {
            const ongoing = await fetchMissionRuns({
                statuses: [MissionStatus.Ongoing, MissionStatus.Paused],
                pageSize: 100,
                orderBy: 'StartTime desc',
            }).catch((e) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve mission runs')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent
                        translatedMessage={TranslateText('Failed to retrieve mission runs')}
                    />,
                    AlertCategory.ERROR
                )
            })

            setOngoingMissions(ongoing ?? [])

            const queue = await fetchMissionRuns({
                statuses: [MissionStatus.Pending],
                pageSize: 100,
                orderBy: 'DesiredStartTime',
            }).catch((e) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve mission runs')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent
                        translatedMessage={TranslateText('Failed to retrieve mission runs')}
                    />,
                    AlertCategory.ERROR
                )
            })

            setMissionQueue(queue ?? [])
        }
        if (BackendAPICaller.accessToken) fetchAndUpdateMissions()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [BackendAPICaller.accessToken])

    const [filteredMissionQueue, setFilteredMissionQueue] = useState<Mission[]>([])
    const [filteredOngoingMissions, setFilteredOngoingMissions] = useState<Mission[]>([])
    useEffect(() => {
        setFilteredOngoingMissions(ongoingMissions.filter((m) => m.area?.installationCode === installationCode))
        setFilteredMissionQueue(missionQueue.filter((m) => m.area?.installationCode === installationCode))
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [installationCode, ongoingMissions, missionQueue])

    return {
        ongoingMissions: filteredOngoingMissions,
        missionQueue: filteredMissionQueue,
        loadingMissionSet,
        setLoadingMissionSet,
    }
}

export const MissionRunsProvider: FC<Props> = ({ children }) => {
    const { ongoingMissions, missionQueue, loadingMissionSet, setLoadingMissionSet } = useMissionRuns()
    return (
        <MissionRunsContext.Provider value={{ ongoingMissions, missionQueue, loadingMissionSet, setLoadingMissionSet }}>
            {children}
        </MissionRunsContext.Provider>
    )
}

export const useMissionsContext = () => useContext(MissionRunsContext)
