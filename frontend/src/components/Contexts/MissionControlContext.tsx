import { createContext, useContext, useState, FC } from 'react'
import { AlertType, useAlertContext } from './AlertContext'
import { useLanguageContext } from './LanguageContext'
import { useAssetContext } from './AssetContext'
import { MissionStatusRequest } from 'pages/FrontPage/MissionOverview/StopDialogs'
import { useBackendApi } from 'api/UseBackendApi'

interface IMissionControlState {
    isRobotMissionWaitingForResponseDict: { [robotId: string]: boolean }
}

interface Props {
    children: React.ReactNode
}

interface IMissionControlContext {
    missionControlState: IMissionControlState
    updateRobotMissionState: (newState: MissionStatusRequest, robotId: string) => void
}

const defaultMissionControlInterface = {
    missionControlState: { isRobotMissionWaitingForResponseDict: {} },
    updateRobotMissionState: () => {},
}

const MissionControlContext = createContext<IMissionControlContext>(defaultMissionControlInterface)
const defaultManagementState: IMissionControlState = {
    isRobotMissionWaitingForResponseDict: {},
}

export const MissionControlProvider: FC<Props> = ({ children }) => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { raiseAlert } = useAlertContext()
    const [missionControlState, setMissionControlState] = useState<IMissionControlState>(defaultManagementState)
    const backendApi = useBackendApi()

    const setIsWaitingForResponse = (robotId: string, isWaiting: boolean) => {
        const updatedDict = { ...missionControlState.isRobotMissionWaitingForResponseDict }
        updatedDict[robotId] = isWaiting
        setMissionControlState({ isRobotMissionWaitingForResponseDict: updatedDict })
    }

    const updateRobotMissionState = (newState: MissionStatusRequest, robotId: string) => {
        const robot = enabledRobots.find((r) => r.id === robotId)
        if (!robot) {
            raiseAlert(AlertType.RequestFail, {
                kind: 'requestFail',
                message: TranslateText('Unable to find robot with ID {0}', [robotId]),
            })
            return
        }

        const robotName = robot!.name!
        switch (newState) {
            case MissionStatusRequest.Pause: {
                setIsWaitingForResponse(robotId, true)
                backendApi
                    .pauseMission(robotId)
                    .catch(() => {
                        raiseAlert(AlertType.RequestFail, {
                            kind: 'requestFail',
                            message: TranslateText('Failed to pause mission on {0}', [robotName]),
                        })
                    })
                    .finally(() => {
                        setIsWaitingForResponse(robotId, false)
                    })
                break
            }
            case MissionStatusRequest.Resume: {
                setIsWaitingForResponse(robotId, true)
                backendApi
                    .resumeMission(robotId)
                    .catch(() => {
                        raiseAlert(AlertType.RequestFail, {
                            kind: 'requestFail',
                            message: TranslateText('Failed to resume mission on {0}', [robotName]),
                        })
                    })
                    .finally(() => {
                        setIsWaitingForResponse(robotId, false)
                    })
                break
            }
            case MissionStatusRequest.Stop: {
                setIsWaitingForResponse(robotId, true)
                backendApi
                    .stopMission(robotId)
                    .catch(() => {
                        raiseAlert(AlertType.RequestFail, {
                            kind: 'requestFail',
                            message: TranslateText('Failed to stop mission on {0}', [robotName]),
                        })
                    })
                    .finally(() => {
                        setIsWaitingForResponse(robotId, false)
                    })
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
