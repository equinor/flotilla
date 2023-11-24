import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { BackendAPICaller } from 'api/ApiCaller'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'

const upsertList = (list: Mission[], mission: Mission) => {
    let newList = [...list]
    const i = newList.findIndex((e) => e.id === mission.id)
    if (i > -1) newList[i] = mission
    else newList.push(mission)
    return newList
}

interface IMissionsContext {
    ongoingMissions: Mission[]
    missionQueue: Mission[]
}

interface Props {
    children: React.ReactNode
}

const defaultMissionsContext: IMissionsContext = {
    ongoingMissions: [],
    missionQueue: [],
}

export const MissionsContext = createContext<IMissionsContext>(defaultMissionsContext)

interface MissionsResult {
    ongoingMissions: Mission[]
    missionQueue: Mission[]
}

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

const fetchMissions = (params: {
    statuses: MissionStatus[]
    pageSize: number
    orderBy: string
}): Promise<Mission[]> => {
    return BackendAPICaller.getMissionRuns(params).then((response) => response.content)
}
export const useMissions = (): MissionsResult => {
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [missionQueue, setMissionQueue] = useState<Mission[]>([])
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
                        missionQueueCopy = upsertList(missionQueueCopy, newMission)
                        return [...missionQueueCopy]
                    })
            })
            registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
                let updatedMission: Mission = JSON.parse(message)
                // This conversion translates from the enum as a number to an enum as a string
                updatedMission.status = Object.values(MissionStatus)[updatedMission.status as unknown as number]

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
                    const ongoingIndex = missions.findIndex((m) => m.id === deletedMission.id)
                    if (ongoingIndex !== -1) missions.splice(ongoingIndex, 1) // Remove deleted mission
                    return missions
                })
                setMissionQueue((missions) => {
                    const queueIndex = missions.findIndex((m) => m.id === deletedMission.id)
                    if (queueIndex !== -1) missions.splice(queueIndex, 1) // Remove deleted mission
                    return missions
                })
            })
        }
    }, [registerEvent, connectionReady, missionQueue])

    useEffect(() => {
        const fetchAndUpdateMissions = async () => {
            const ongoing = await fetchMissions({
                statuses: [MissionStatus.Ongoing, MissionStatus.Paused],
                pageSize: 100,
                orderBy: 'StartTime desc',
            })
            setOngoingMissions(ongoing)

            const queue = await fetchMissions({
                statuses: [MissionStatus.Pending],
                pageSize: 100,
                orderBy: 'DesiredStartTime',
            })
            setMissionQueue(queue)
        }

        if (BackendAPICaller.accessToken) fetchAndUpdateMissions()
        // Need to include BackendAPICaller.accessToken in dependency array to ensure initial render
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [BackendAPICaller.accessToken])

    return { ongoingMissions, missionQueue }
}

export const MissionsProvider: FC<Props> = ({ children }) => {
    const { ongoingMissions, missionQueue } = useMissions()
    return <MissionsContext.Provider value={{ ongoingMissions, missionQueue }}>{children}</MissionsContext.Provider>
}

export const useMissionsContext = () => useContext(MissionsContext)
