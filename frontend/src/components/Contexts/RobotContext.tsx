import { createContext, useContext, useState, FC, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot, RobotStatus } from 'models/Robot'
import { useSignalRContext } from './SignalRContext'
import { BatteryStatus } from 'models/Battery'
import { RobotType } from 'models/RobotModel'

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
            registerEvent('robot list updated', (username: string, message: string) => {
                let newRobotList: Robot[] = JSON.parse(message)
                newRobotList = newRobotList.map((r) => {
                    r.status = Object.values(RobotStatus)[r.status as unknown as number]
                    r.batteryStatus = Object.values(BatteryStatus)[r.batteryStatus as unknown as number]
                    r.model.type = Object.values(RobotType)[r.model.type as unknown as number]
                    return r
                })
                setEnabledRobots(newRobotList)
            })
        }
    }, [registerEvent, connectionReady])

    useEffect(() => {
        if (!enabledRobots || enabledRobots.length === 0)
            BackendAPICaller.getEnabledRobots().then((robots) => {
                setEnabledRobots(robots)
            })
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
