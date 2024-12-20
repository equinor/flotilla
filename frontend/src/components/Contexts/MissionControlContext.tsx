import { createContext, useContext, useState, FC } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { MissionStatusRequest } from 'components/Pages/FrontPage/MissionOverview/StopDialogs'
import { AlertType, useAlertContext } from './AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useLanguageContext } from './LanguageContext'
import { useRobotContext } from './RobotContext'

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
    const { enabledRobots } = useRobotContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [missionControlState, setMissionControlState] = useState<IMissionControlState>(defaultManagementState)

    const setIsWaitingForResponse = (robotId: string, isWaiting: boolean) => {
        const updatedDict = { ...missionControlState.isRobotMissionWaitingForResponseDict }
        updatedDict[robotId] = isWaiting
        setMissionControlState({ isRobotMissionWaitingForResponseDict: updatedDict })
    }

    const updateRobotMissionState = (newState: MissionStatusRequest, robotId: string) => {
        const robot = enabledRobots.find((r) => r.id === robotId)
        if (!robot) {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent
                    translatedMessage={TranslateText('Unable to find robot with ID {0}', [robotId])}
                />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Unable to find robot with ID {0}', [robotId])}
                />,
                AlertCategory.ERROR
            )
            return
        }

        const robotName = robot!.name!
        switch (newState) {
            case MissionStatusRequest.Pause: {
                setIsWaitingForResponse(robotId, true)
                BackendAPICaller.pauseMission(robotId)
                    .then(() => setIsWaitingForResponse(robotId, false))
                    .catch(() => {
                        setAlert(
                            AlertType.RequestFail,
                            <FailedRequestAlertContent
                                translatedMessage={TranslateText('Failed to pause mission on {0}', [robotName])}
                            />,
                            AlertCategory.ERROR
                        )
                        setListAlert(
                            AlertType.RequestFail,
                            <FailedRequestAlertListContent
                                translatedMessage={TranslateText('Failed to pause mission on {0}', [robotName])}
                            />,
                            AlertCategory.ERROR
                        )
                    })
                break
            }
            case MissionStatusRequest.Resume: {
                setIsWaitingForResponse(robotId, true)
                BackendAPICaller.resumeMission(robotId)
                    .then(() => setIsWaitingForResponse(robotId, false))
                    .catch(() => {
                        setAlert(
                            AlertType.RequestFail,
                            <FailedRequestAlertContent
                                translatedMessage={TranslateText('Failed to resume mission on {0}', [robotName])}
                            />,
                            AlertCategory.ERROR
                        )
                        setListAlert(
                            AlertType.RequestFail,
                            <FailedRequestAlertListContent
                                translatedMessage={TranslateText('Failed to resume mission on {0}', [robotName])}
                            />,
                            AlertCategory.ERROR
                        )
                    })
                break
            }
            case MissionStatusRequest.Stop: {
                setIsWaitingForResponse(robotId, true)
                BackendAPICaller.stopMission(robotId)
                    .then(() => setIsWaitingForResponse(robotId, false))
                    .catch(() => {
                        setAlert(
                            AlertType.RequestFail,
                            <FailedRequestAlertContent
                                translatedMessage={TranslateText('Failed to stop mission on {0}', [robotName])}
                            />,
                            AlertCategory.ERROR
                        )
                        setListAlert(
                            AlertType.RequestFail,
                            <FailedRequestAlertListContent
                                translatedMessage={TranslateText('Failed to stop mission on {0}', [robotName])}
                            />,
                            AlertCategory.ERROR
                        )
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
