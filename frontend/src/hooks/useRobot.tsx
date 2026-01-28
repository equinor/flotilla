import { useState, useEffect } from 'react'
import { Robot, RobotPropertyUpdate } from 'models/Robot'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { BackendAPICaller } from 'api/ApiCaller'

export const useRobot = (robotId: string) => {
    const [isFetchingRobot, setIsFetchingRobot] = useState<boolean>(true)
    const [robot, setRobot] = useState<Robot | undefined>(undefined)
    const { registerEvent, connectionReady } = useSignalRContext()
    let batteryReadingTimer: NodeJS.Timeout
    let pressureReadingTimer: NodeJS.Timeout

    useEffect(() => {
        setIsFetchingRobot(true)
        BackendAPICaller.getRobotById(robotId)
            .then((fetchedRobot: Robot) => {
                setIsFetchingRobot(false)
                setRobot(fetchedRobot)
            })
            .catch((error) => {
                console.error(`Failed to fetch robot with id ${robotId}:`, error)
                setIsFetchingRobot(false)
                setRobot(undefined)
            })
    }, [robotId])

    const clearBatteryLevel = () => {
        setRobot((oldRobot) => {
            if (!oldRobot) return oldRobot
            return {
                ...oldRobot,
                batteryLevel: undefined,
            }
        })
    }

    const clearPressureLevel = () => {
        setRobot((oldRobot) => {
            if (!oldRobot) return oldRobot
            return {
                ...oldRobot,
                pressureLevel: undefined,
            }
        })
    }

    useEffect(() => {
        if (connectionReady && !isFetchingRobot) {
            if (robot === undefined) {
                console.error(
                    `We should have fetched the robot, but the robot is not fetched ${robotId}. Might it be deleted?`
                )
                return
            }
            registerEvent(SignalREventLabels.robotUpdated, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                if (updatedRobot.id === robotId) {
                    setRobot(updatedRobot)
                }
            })
            registerEvent(SignalREventLabels.robotPropertyUpdated, (username: string, message: string) => {
                const robotPropertyUpdate: RobotPropertyUpdate = JSON.parse(message)
                if (robotPropertyUpdate.robotId === robotId) {
                    setRobot((oldRobot) => {
                        if (!oldRobot) return oldRobot
                        return {
                            ...oldRobot,
                            [robotPropertyUpdate.propertyName]: robotPropertyUpdate.propertyValue,
                        }
                    })

                    if (robotPropertyUpdate.propertyName === 'batteryLevel') {
                        clearTimeout(batteryReadingTimer)
                        batteryReadingTimer = setTimeout(clearBatteryLevel, 30 * 1000) // Time in milliseconds
                    } else if (robotPropertyUpdate.propertyName === 'pressureLevel') {
                        clearTimeout(pressureReadingTimer)
                        pressureReadingTimer = setTimeout(clearPressureLevel, 30 * 1000) // Time in milliseconds
                    }
                }
            })
            registerEvent(SignalREventLabels.robotDeleted, (username: string, message: string) => {
                const updatedRobot: Robot = JSON.parse(message)
                if (updatedRobot.id === robotId) {
                    setRobot(undefined)
                }
            })
        }
    }, [registerEvent, connectionReady, isFetchingRobot, robotId])

    return robot
}
