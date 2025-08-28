import { createContext, useContext, useState, FC, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
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

interface Props {
    children: React.ReactNode
}

interface IRobotContext {
    enabledRobots: Robot[]
}

const defaultRobotState = {
    enabledRobots: [],
}

interface RobotPropertyUpdate {
    robotId: string
    propertyName: string
    propertyValue: any
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
                updatedRobot.batteryLevel = undefined
                updatedRobot.batteryState = undefined
                updatedRobot.pressureLevel = undefined
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
            registerEvent(SignalREventLabels.robotPropertyUpdated, (username: string, message: string) => {
                const robotPropertyUpdate: RobotPropertyUpdate = JSON.parse(message)
                setEnabledRobots((oldRobotList) => {
                    const oldRobotListCopy = [...oldRobotList]
                    const index = oldRobotListCopy.findIndex((r) => r.id === robotPropertyUpdate.robotId)
                    if (index > -1) {
                        const robot = oldRobotListCopy[index]
                        if (robotPropertyUpdate.propertyName in robot) {
                            ;(robot as any)[robotPropertyUpdate.propertyName] = robotPropertyUpdate.propertyValue
                            oldRobotListCopy[index] = robot
                        } else {
                            console.warn(`Property ${robotPropertyUpdate.propertyName} does not exist on Robot`)
                        }
                    }
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
                    robots.forEach((r) => {
                        r.batteryLevel = undefined
                        r.batteryState = undefined
                        r.pressureLevel = undefined
                    })
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
