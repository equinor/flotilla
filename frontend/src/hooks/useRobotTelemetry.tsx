import { useState, useEffect } from 'react'
import { RobotWithoutTelemetry, RobotTelemetryPropertyUpdate } from 'models/Robot'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { Pose } from 'models/Pose'
import { BatteryStatus } from 'models/Battery'

export const useRobotTelemetry = (robotWithoutDetails: RobotWithoutTelemetry) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const [robotBatteryLevel, setRobotBatteryLevel] = useState<number | undefined>(undefined)
    const [robotBatteryStatus, setRobotBatteryStatus] = useState<BatteryStatus | undefined>(undefined)
    const [robotPressureLevel, setRobotPressureLevel] = useState<number | undefined>(undefined)
    const [robotPose, setRobotPose] = useState<Pose | undefined>(undefined)

    const robotId = robotWithoutDetails.id
    let batteryReadingTimer: NodeJS.Timeout
    let pressureReadingTimer: NodeJS.Timeout

    const clearBatteryLevel = () => {
        setRobotBatteryLevel(undefined)
    }

    const clearPressureLevel = () => {
        setRobotPressureLevel(undefined)
    }

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.robotPropertyUpdated, (username: string, message: string) => {
                const robotPropertyUpdate: RobotTelemetryPropertyUpdate = JSON.parse(message)
                if (robotPropertyUpdate.robotId === robotId) {
                    if (robotPropertyUpdate.propertyName === 'batteryLevel') {
                        setRobotBatteryLevel(robotPropertyUpdate.propertyValue as number)
                        clearTimeout(batteryReadingTimer)
                        batteryReadingTimer = setTimeout(clearBatteryLevel, 30 * 1000) // Time in milliseconds
                    } else if (robotPropertyUpdate.propertyName === 'pressureLevel') {
                        setRobotPressureLevel(robotPropertyUpdate.propertyValue as number)
                        clearTimeout(pressureReadingTimer)
                        pressureReadingTimer = setTimeout(clearPressureLevel, 30 * 1000) // Time in milliseconds
                    } else if (robotPropertyUpdate.propertyName === 'batteryState') {
                        setRobotBatteryStatus(robotPropertyUpdate.propertyValue as BatteryStatus)
                    } else if (robotPropertyUpdate.propertyName === 'pose') {
                        setRobotPose(robotPropertyUpdate.propertyValue as Pose)
                    }
                }
            })
        }
    }, [connectionReady, robotWithoutDetails])

    return { robotBatteryLevel, robotBatteryStatus, robotPressureLevel, robotPose }
}
