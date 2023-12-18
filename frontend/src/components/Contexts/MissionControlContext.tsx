import { createContext, useContext, useState, FC } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { MissionStatusRequest } from 'components/Pages/FrontPage/MissionOverview/StopDialogs'

interface IMissionControlState {
    isWaitingForResponse: boolean
}

interface Props {
    children: React.ReactNode
}

export interface IMissionControlContext {
    missionControlState: IMissionControlState
    updateRobotMissionState: (newState: MissionStatusRequest, robotId: string) => void
}

const defaultMissionControlInterface = {
    missionControlState: { isWaitingForResponse: false },
    updateRobotMissionState: (newState: MissionStatusRequest, robotId: string) => {},
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

    const updateRobotMissionState = (newState: MissionStatusRequest, robotId: string) => {
        switch (newState) {
            case MissionStatusRequest.Pause: {
                setIsWaitingForResponse(true)
                BackendAPICaller.pauseMission(robotId).then((_) => setIsWaitingForResponse(false))
                break
            }
            case MissionStatusRequest.Resume: {
                setIsWaitingForResponse(true)
                BackendAPICaller.resumeMission(robotId).then((_) => setIsWaitingForResponse(false))
                break
            }
            case MissionStatusRequest.Stop: {
                setIsWaitingForResponse(true)
                BackendAPICaller.stopMission(robotId).then((_) => setIsWaitingForResponse(false))
                break
            }
        }
    }

    return (
        <MissionControlContext.Provider
            value={{
                missionControlState,
                updateRobotMissionState,
            }}
        >
            {children}
        </MissionControlContext.Provider>
    )
}

export const useMissionControlContext = () => useContext(MissionControlContext) as IMissionControlContext
