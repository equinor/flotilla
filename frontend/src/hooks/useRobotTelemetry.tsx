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
    let batteryReadingTimerId: number
    let pressureReadingTimerId: number

    const clearBatteryLevel = () => {
        setRobotBatteryLevel(undefined)
    }

    const clearPressureLevel = () => {
        setRobotPressureLevel(undefined)
    }

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.robotTelemetryUpdated, (username: string, message: string) => {
                const robotPropertyUpdate: RobotTelemetryPropertyUpdate = JSON.parse(message)
                if (robotPropertyUpdate.robotId === robotId) {
                    if (robotPropertyUpdate.telemetryName === 'batteryLevel') {
                        setRobotBatteryLevel(robotPropertyUpdate.telemetryValue as number)
                        clearTimeout(batteryReadingTimerId)
                        batteryReadingTimerId = setTimeout(clearBatteryLevel, 30 * 1000) // Time in milliseconds
                    } else if (robotPropertyUpdate.telemetryName === 'pressureLevel') {
                        setRobotPressureLevel(robotPropertyUpdate.telemetryValue as number)
                        clearTimeout(pressureReadingTimerId)
                        pressureReadingTimerId = setTimeout(clearPressureLevel, 30 * 1000) // Time in milliseconds
                    } else if (robotPropertyUpdate.telemetryName === 'batteryState') {
                        setRobotBatteryStatus(robotPropertyUpdate.telemetryValue as BatteryStatus)
                    } else if (robotPropertyUpdate.telemetryName === 'pose') {
                        setRobotPose(robotPropertyUpdate.telemetryValue as Pose)
                    }
                }
            })
        }
    }, [connectionReady, robotWithoutDetails])

    return { robotBatteryLevel, robotBatteryStatus, robotPressureLevel, robotPose }
}
