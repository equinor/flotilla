import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { refreshInterval } from 'components/Pages/FrontPage/FrontPage'
import { BackendAPICaller } from 'api/ApiCaller'

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
export const useMissions = (refreshInterval: number): MissionsResult => {
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [missionQueue, setMissionQueue] = useState<Mission[]>([])

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

        const id = setInterval(fetchAndUpdateMissions, refreshInterval)

        return () => clearInterval(id)
    }, [refreshInterval, BackendAPICaller.accessToken])

    return { ongoingMissions, missionQueue }
}
export const MissionsProvider: FC<Props> = ({ children }) => {
    const { ongoingMissions, missionQueue } = useMissions(refreshInterval)

    return <MissionsContext.Provider value={{ ongoingMissions, missionQueue }}>{children}</MissionsContext.Provider>
}

export const useMissionsContext = () => useContext(MissionsContext)
