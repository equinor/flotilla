import { createContext, FC, useContext, useEffect, useState } from 'react'
import { Mission, MissionStatus } from 'models/Mission'
import { refreshInterval } from 'components/Pages/FrontPage/FrontPage'
import { BackendAPICaller } from 'api/ApiCaller'

interface IMissionOngoingContext {
    missionOngoing: Mission[]
}

interface Props {
    children: React.ReactNode
}

const defaultMissionOngoingeInterface = {
    missionOngoing: [],
}

export const MissionOngoingContext = createContext<IMissionOngoingContext>(defaultMissionOngoingeInterface)

export const MissionOngoingProvider: FC<Props> = ({ children }) => {
    const missionPageSize = 100
    const [missionOngoing, setmissionOngoing] = useState<Mission[]>(defaultMissionOngoingeInterface.missionOngoing)
    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getMissionRuns({
                statuses: [MissionStatus.Ongoing, MissionStatus.Paused],
                pageSize: missionPageSize,
                orderBy: 'StartTime desc',
            }).then((missions) => {
                setmissionOngoing(missions.content)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    return (
        <MissionOngoingContext.Provider
            value={{
                missionOngoing,
            }}
        >
            {children}
        </MissionOngoingContext.Provider>
    )
}

export const useMissionOngoingContext = () => useContext(MissionOngoingContext)
