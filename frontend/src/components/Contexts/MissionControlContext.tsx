import { createContext, useContext, useState, FC } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Mission } from 'models/Mission'
import { MissionStatusRequest } from 'components/Pages/FrontPage/MissionOverview/StopDialogs'

interface IMissionControlState {
    isWaitingForResponse: boolean
}

interface Props {
    children: React.ReactNode
}

export interface IMissionControlContext {
    missionControlState: IMissionControlState
    updateMissionState: (newState: MissionStatusRequest, mission: Mission) => void
}

const defaultMissionControlInterface = {
    missionControlState: { isWaitingForResponse: false },
    updateMissionState: (newState: MissionStatusRequest, mission: Mission) => {},
}

export const MissionControlContext = createContext<IMissionControlContext>(defaultMissionControlInterface)
const defaultManagementState: IMissionControlState = {
    isWaitingForResponse: false,
}

export const MissionControlProvider: FC<Props> = ({ children }) => {
    const [missionControlState, setMissionControlState] = useState<IMissionControlState>(defaultManagementState)

    const setIsWaitingForResponse = (isWaiting: boolean) => {
        setMissionControlState({ isWaitingForResponse: isWaiting })
    }

    const updateMissionState = (newState: MissionStatusRequest, mission: Mission) => {
        switch (newState) {
            case MissionStatusRequest.Pause: {
                setIsWaitingForResponse(true)
                BackendAPICaller.pauseMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                break
            }
            case MissionStatusRequest.Resume: {
                setIsWaitingForResponse(true)
                BackendAPICaller.resumeMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                break
            }
            case MissionStatusRequest.Stop: {
                setIsWaitingForResponse(true)
                BackendAPICaller.stopMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                break
            }
        }
    }

    return (
        <MissionControlContext.Provider
            value={{
                missionControlState,
                updateMissionState,
            }}
        >
            {children}
        </MissionControlContext.Provider>
    )
}

export const useMissionControlContext = () => useContext(MissionControlContext) as IMissionControlContext
