import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { BackendAPICaller } from 'api/ApiCaller'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { translateSignalRMission } from 'utils/EnumTranslations'

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

export const MissionRunsContext = createContext<IMissionRunsContext>(defaultMissionRunsContext)

const updateQueueIfMissionAlreadyQueued = (oldQueue: Mission[], updatedMission: Mission) => {
    const existingMissionIndex = oldQueue.findIndex((m) => m.id === updatedMission.id)
    if (existingMissionIndex !== -1) {
        // If the mission is already in the queue
        if (updatedMission.status !== MissionStatus.Pending) oldQueue.splice(existingMissionIndex, 1)
        else oldQueue[existingMissionIndex] = updatedMission
    }
    return oldQueue
}

const updateOngoingMissionsWithUpdatedMission = (oldMissionList: Mission[], updatedMission: Mission) => {
    const existingMissionIndex = oldMissionList.findIndex((m) => m.id === updatedMission.id)
    if (updatedMission.status === MissionStatus.Ongoing || updatedMission.status === MissionStatus.Paused) {
        if (existingMissionIndex !== -1) {
            // Mission is ongoing and in the queue
            oldMissionList[existingMissionIndex] = updatedMission
        } else {
            // Mission is ongoing and not in the queue
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

export const useMissionRuns = (): IMissionRunsContext => {
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [missionQueue, setMissionQueue] = useState<Mission[]>([])
    const [loadingMissionSet, setLoadingMissionSet] = useState<Set<string>>(new Set())
    const { registerEvent, connectionReady } = useSignalRContext()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunCreated, (username: string, message: string) => {
                const newMission: Mission = JSON.parse(message)
                if (!missionQueue.find((m) => m.id === newMission.id))
                    setMissionQueue((oldQueue) => [...oldQueue, newMission])
                else
                    setMissionQueue((oldQueue) => {
                        let missionQueueCopy = [...oldQueue]
                        missionQueueCopy = upsertMissionList(missionQueueCopy, newMission)
                        return [...missionQueueCopy]
                    })
            })
            registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
                let updatedMission: Mission = JSON.parse(message)
                updatedMission = translateSignalRMission(updatedMission)

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
            })
            setOngoingMissions(ongoing)

            const queue = await fetchMissionRuns({
                statuses: [MissionStatus.Pending],
                pageSize: 100,
                orderBy: 'DesiredStartTime',
            })
            setMissionQueue(queue)
        }
        if (BackendAPICaller.accessToken) fetchAndUpdateMissions()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [BackendAPICaller.accessToken])

    return { ongoingMissions, missionQueue, loadingMissionSet, setLoadingMissionSet }
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
