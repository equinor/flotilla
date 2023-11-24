import { createContext, useContext, useState, FC, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot, RobotStatus } from 'models/Robot'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { BatteryStatus } from 'models/Battery'
import { RobotType } from 'models/RobotModel'

const upsertRobotList = (list: Robot[], mission: Robot) => {
    let newList = [...list]
    const i = newList.findIndex((e) => e.id === mission.id)
    if (i > -1) newList[i] = mission
    else newList.push(mission)
    return newList
}

interface Props {
    children: React.ReactNode
}

export interface IRobotContext {
    enabledRobots: Robot[]
}

const defaultRobotState = {
    enabledRobots: [],
}

export const RobotContext = createContext<IRobotContext>(defaultRobotState)

export const RobotProvider: FC<Props> = ({ children }) => {
    const [enabledRobots, setEnabledRobots] = useState<Robot[]>(defaultRobotState.enabledRobots)
    const { registerEvent, connectionReady } = useSignalRContext()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.robotAdded, (username: string, message: string) => {
                let updatedRobot: Robot = JSON.parse(message)
                updatedRobot = {
                    ...updatedRobot,
                    status: Object.values(RobotStatus)[updatedRobot.status as unknown as number],
                    batteryStatus: Object.values(BatteryStatus)[updatedRobot.batteryStatus as unknown as number],
                    model: {
                        ...updatedRobot.model,
                        type: Object.values(RobotType)[updatedRobot.model.type as unknown as number],
                    },
                }
                setEnabledRobots((oldRobotList) => {
                    let oldRobotListCopy = [...oldRobotList]
                    oldRobotListCopy = upsertRobotList(oldRobotListCopy, updatedRobot)
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotUpdated, (username: string, message: string) => {
                let updatedRobot: Robot = JSON.parse(message)
                updatedRobot = {
                    ...updatedRobot,
                    status: Object.values(RobotStatus)[updatedRobot.status as unknown as number],
                    batteryStatus: Object.values(BatteryStatus)[updatedRobot.batteryStatus as unknown as number],
                    model: {
                        ...updatedRobot.model,
                        type: Object.values(RobotType)[updatedRobot.model.type as unknown as number],
                    },
                }
                setEnabledRobots((oldRobotList) => {
                    let oldRobotListCopy = [...oldRobotList]
                    oldRobotListCopy = upsertRobotList(oldRobotListCopy, updatedRobot)
                    return [...oldRobotListCopy]
                })
            })
            registerEvent(SignalREventLabels.robotDeleted, (username: string, message: string) => {
                let updatedRobot: Robot = JSON.parse(message)
                updatedRobot = {
                    ...updatedRobot,
                    status: Object.values(RobotStatus)[updatedRobot.status as unknown as number],
                    batteryStatus: Object.values(BatteryStatus)[updatedRobot.batteryStatus as unknown as number],
                    model: {
                        ...updatedRobot.model,
                        type: Object.values(RobotType)[updatedRobot.model.type as unknown as number],
                    },
                }
                setEnabledRobots((oldRobotList) => {
                    let newRobotList = [...oldRobotList]
                    const index = newRobotList.findIndex((r) => r.id === updatedRobot.id)
                    if (index !== -1) newRobotList.splice(index, 1) // Remove deleted robot
                    return newRobotList
                })
            })
        }
    }, [registerEvent, connectionReady])

    useEffect(() => {
        if (!enabledRobots || enabledRobots.length === 0)
            BackendAPICaller.getEnabledRobots().then((robots) => {
                setEnabledRobots(robots)
            })
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    return (
        <RobotContext.Provider
            value={{
                enabledRobots,
            }}
        >
            {children}
        </RobotContext.Provider>
    )
}

export const useRobotContext = () => useContext(RobotContext) as IRobotContext
