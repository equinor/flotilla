import { createContext, useContext, useState, FC, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot, RobotAttributeUpdate } from 'models/Robot'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { useLanguageContext } from './LanguageContext'
import { AlertType, useAlertContext } from './AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useInstallationContext } from './InstallationContext'

const upsertRobotList = (list: Robot[], robot: Robot) => {
    const newList = [...list]
    const i = newList.findIndex((e) => e.id === robot.id)
    if (i > -1) newList[i] = robot
    else newList.push(robot)
    return newList
}

const updateRobotInList = (list: Robot[], robotId: string, mapping: (old: Robot) => Robot) => {
    const newList = [...list]
    const i = newList.findIndex((e) => e.id === robotId)
    if (i > -1) newList[i] = mapping(newList[i])
    return newList
}

interface Props {
    children: React.ReactNode
}

interface IRobotContext {
    enabledRobots: Robot[]
}

const defaultRobotState = {
    enabledRobots: [],
}

const RobotContext = createContext<IRobotContext>(defaultRobotState)

export const RobotProvider: FC<Props> = ({ children }) => {
    const [enabledRobots, setEnabledRobots] = useState<Robot[]>(defaultRobotState.enabledRobots)
    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const { installationCode } = useInstallationContext()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.robotAdded, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                setEnabledRobots((oldRobotList) => {
                    let oldRobotListCopy = [...oldRobotList]
                    oldRobotListCopy = upsertRobotList(oldRobotListCopy, updatedRobot)
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotUpdated, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                // The check below makes it so that it is not treated as null in the code.
                if (updatedRobot.model.type == null) {
                    console.warn('Received robot update with model type null')
                    return
                }
                setEnabledRobots((oldRobotList) => {
                    let oldRobotListCopy = [...oldRobotList]
                    oldRobotListCopy = upsertRobotList(oldRobotListCopy, updatedRobot)
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotAttributeUpdated, (username: string, message: string) => {
                const updatedRobot: RobotAttributeUpdate = JSON.parse(message)
                // The check below makes it so that it is not treated as null in the code.

                const updatedProperty = updatedRobot.propertyName
                if (!updatedProperty) return

                const updatedValue = updatedRobot.value

                const updateFunction = (oldRobot: Robot): Robot => {
                    if (Object.keys(oldRobot).includes(updatedProperty))
                        oldRobot = { ...oldRobot, [updatedProperty]: updatedValue }
                    return oldRobot
                }
                setEnabledRobots((oldRobotList) => {
                    let oldRobotListCopy = [...oldRobotList]
                    oldRobotListCopy = updateRobotInList(oldRobotListCopy, updatedRobot.id, updateFunction)
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotDeleted, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                setEnabledRobots((oldRobotList) => {
                    const newRobotList = [...oldRobotList]
                    const index = newRobotList.findIndex((r) => r.id === updatedRobot.id)
                    if (index !== -1) newRobotList.splice(index, 1) // Remove deleted robot
                    return newRobotList
                })
            })
        }
    }, [registerEvent, connectionReady])

    useEffect(() => {
        if (!enabledRobots || enabledRobots.length === 0)
            BackendAPICaller.getEnabledRobots()
                .then((robots) => {
                    setEnabledRobots(robots)
                })
                .catch(() => {
                    setAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve robots')} />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertListContent
                            translatedMessage={TranslateText('Failed to retrieve robots')}
                        />,
                        AlertCategory.ERROR
                    )
                })
    }, [])

    const [filteredRobots, setFilteredRobots] = useState<Robot[]>([])

    useEffect(() => {
        setFilteredRobots(
            enabledRobots.filter(
                (r) => r.currentInstallation.installationCode.toLowerCase() === installationCode.toLowerCase()
            )
        )
    }, [installationCode, enabledRobots])

    return (
        <RobotContext.Provider
            value={{
                enabledRobots: filteredRobots,
            }}
        >
            {children}
        </RobotContext.Provider>
    )
}

export const useRobotContext = () => useContext(RobotContext) as IRobotContext
