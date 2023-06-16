import { createContext, useContext, useState, FC } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Mission } from 'models/Mission'
import { ControlButton } from 'components/Pages/FrontPage/MissionOverview/MissionControlButtons'

interface IMissionControlState {
    isWaitingForResponse: boolean
}

interface Props {
    children: React.ReactNode
}

export interface IMissionControlContext {
    missionControlState: IMissionControlState
    setIsWaitingForResponse: (isWaiting: boolean) => void
    handleClick: (button: ControlButton, mission: Mission) => void
}

const defaultMissionControlInterface = {
    missionControlState: { isWaitingForResponse: false },
    setIsWaitingForResponse: (isWaiting: boolean) => {},
    handleClick: (button: ControlButton, mission: Mission) => {},
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

    const handleClick = (button: ControlButton, mission: Mission) => {
        switch (button) {
            case ControlButton.Pause: {
                setIsWaitingForResponse(true)
                BackendAPICaller.pauseMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                break
            }
            case ControlButton.Resume: {
                setIsWaitingForResponse(true)
                BackendAPICaller.resumeMission(mission.robot.id).then((_) => setIsWaitingForResponse(false))
                break
            }
            case ControlButton.Stop: {
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
                setIsWaitingForResponse,
                handleClick,
            }}
        >
            {children}
        </MissionControlContext.Provider>
    )
}

export const useMissionControlContext = () => useContext(MissionControlContext) as IMissionControlContext
