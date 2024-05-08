import { createContext, useContext, useState, FC } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { MissionStatusRequest } from 'components/Pages/FrontPage/MissionOverview/StopDialogs'

interface IMissionControlState {
    isRobotMissionWaitingForResponseDict: { [robotId: string]: boolean }
}

interface Props {
    children: React.ReactNode
}

export interface IMissionControlContext {
    missionControlState: IMissionControlState
    updateRobotMissionState: (newState: MissionStatusRequest, robotId: string) => void
}

const defaultMissionControlInterface = {
    missionControlState: { isRobotMissionWaitingForResponseDict: {} },
    updateRobotMissionState: (newState: MissionStatusRequest, robotId: string) => {},
}

export const MissionControlContext = createContext<IMissionControlContext>(defaultMissionControlInterface)
const defaultManagementState: IMissionControlState = {
    isRobotMissionWaitingForResponseDict: {},
}

export const MissionControlProvider: FC<Props> = ({ children }) => {
    const [missionControlState, setMissionControlState] = useState<IMissionControlState>(defaultManagementState)

    const setIsWaitingForResponse = (robotId: string, isWaiting: boolean) => {
        const updatedDict = { ...missionControlState.isRobotMissionWaitingForResponseDict }
        updatedDict[robotId] = isWaiting
        setMissionControlState({ isRobotMissionWaitingForResponseDict: updatedDict })
    }

    const updateRobotMissionState = (newState: MissionStatusRequest, robotId: string) => {
        switch (newState) {
            case MissionStatusRequest.Pause: {
                setIsWaitingForResponse(robotId, true)
                BackendAPICaller.pauseMission(robotId).then((_) => setIsWaitingForResponse(robotId, false))
                break
            }
            case MissionStatusRequest.Resume: {
                setIsWaitingForResponse(robotId, true)
                BackendAPICaller.resumeMission(robotId).then((_) => setIsWaitingForResponse(robotId, false))
                break
            }
            case MissionStatusRequest.Stop: {
                setIsWaitingForResponse(robotId, true)
                BackendAPICaller.stopMission(robotId).then((_) => setIsWaitingForResponse(robotId, false))
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
