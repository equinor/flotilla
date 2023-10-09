import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { BackendAPICaller } from 'api/ApiCaller'
import { useSignalRContext } from './SignalRContext'

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
            registerEvent('mission run created', (username: string, message: string) => {
                const newMission = JSON.parse(message)
                if (missionQueue.find((m) => m.id === newMission.id))
                    setMissionQueue((oldQueue) => [...oldQueue, newMission])
                else
                    setMissionQueue((oldQueue) => {
                        let missionQueueCopy = [...oldQueue]
                        missionQueueCopy = upsertList(missionQueueCopy, newMission)
                        return [...missionQueueCopy, newMission]
                    })
            })
            registerEvent('mission run updated', (username: string, message: string) => {
                let updatedMission: Mission = JSON.parse(message)
                // This conversion translates from the enum as a number to an enum as a string
                updatedMission.status = Object.values(MissionStatus)[updatedMission.status as unknown as number]

                setMissionQueue((oldQueue) => {
                    const oldQueueCopy = [...oldQueue]
                    const existingMissionIndex = oldQueue.findIndex((m) => m.id === updatedMission.id)
                    if (existingMissionIndex !== -1) {
                        if (updatedMission.status !== MissionStatus.Pending)
                            oldQueueCopy.splice(existingMissionIndex, 1)
                        else oldQueueCopy[existingMissionIndex] = updatedMission
                    }
                    return oldQueueCopy
                })
                setOngoingMissions((oldQueue) => {
                    const oldQueueCopy = [...oldQueue]
                    const existingMissionIndex = oldQueue.findIndex((m) => m.id === updatedMission.id)
                    if (existingMissionIndex !== -1) {
                        if (
                            updatedMission.status !== MissionStatus.Ongoing &&
                            updatedMission.status !== MissionStatus.Paused
                        )
                            oldQueueCopy.splice(existingMissionIndex, 1)
                        else oldQueueCopy[existingMissionIndex] = updatedMission
                    } else if (
                        updatedMission.status === MissionStatus.Ongoing ||
                        updatedMission.status === MissionStatus.Paused
                    ) {
                        return [...oldQueueCopy, updatedMission]
                    }
                    return oldQueueCopy
                })
            })
        }
    }, [registerEvent, connectionReady])

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
    }, [BackendAPICaller.accessToken])

    return { ongoingMissions, missionQueue }
}

export const MissionsProvider: FC<Props> = ({ children }) => {
    const { ongoingMissions, missionQueue } = useMissions()
    return <MissionsContext.Provider value={{ ongoingMissions, missionQueue }}>{children}</MissionsContext.Provider>
}

export const useMissionsContext = () => useContext(MissionsContext)
