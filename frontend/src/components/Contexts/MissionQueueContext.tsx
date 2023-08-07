import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { refreshInterval } from 'components/Pages/FrontPage/FrontPage'
import { BackendAPICaller } from 'api/ApiCaller'

interface IMissionQueueContext {
    missionQueue: Mission[]
}

interface Props {
    children: React.ReactNode
}

const defaultMissionQueueInterface = {
    missionQueue: [],
}

export const MissionQueueContext = createContext<IMissionQueueContext>(defaultMissionQueueInterface)

export const MissionQueueProvider: FC<Props> = ({ children }) => {
    const missionPageSize = 100
    const [missionQueue, setMissionQueue] = useState<Mission[]>(defaultMissionQueueInterface.missionQueue)

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getMissionRuns({
                statuses: [MissionStatus.Pending],
                pageSize: missionPageSize,
                orderBy: 'DesiredStartTime',
            }).then((missions) => {
                setMissionQueue(missions.content)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    // If we ever want a view which integrates the control of the
    // current mission with the queue, then we could combine this
    // context with MissionControlContext.tsx

    return (
        <MissionQueueContext.Provider
            value={{
                missionQueue,
            }}
        >
            {children}
        </MissionQueueContext.Provider>
    )
}

export const useMissionQueueContext = () => useContext(MissionQueueContext)
