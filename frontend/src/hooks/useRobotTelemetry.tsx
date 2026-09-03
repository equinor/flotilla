import { useState, useEffect, useRef } from 'react'
import { RobotWithoutTelemetry, RobotTelemetryPropertyUpdate } from 'models/Robot'
import { SignalREventLabels, useSignalRContext } from 'contexts/SignalRContext'
import { Pose } from 'models/Pose'
import { BatteryStatus } from 'models/Battery'
import { useAssetContext } from 'contexts/AssetContext'

export const useRobotTelemetry = (robotWithoutDetails: RobotWithoutTelemetry) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const [robotBatteryLevel, setRobotBatteryLevel] = useState<number | undefined>(undefined)
    const [robotBatteryStatus, setRobotBatteryStatus] = useState<BatteryStatus | undefined>(undefined)
    const [robotPressureLevel, setRobotPressureLevel] = useState<number | undefined>(undefined)
    const [robotPose, setRobotPose] = useState<Pose | undefined>(undefined)

    const robotId = robotWithoutDetails.id

    // Refs rather than plain locals. Each registered handler used to close over its own
    // `let` binding, so set and clear stayed paired and cancellation did work; the lint
    // rule flags the pattern because the handle does not survive a re-render, which only
    // bites once more than one handler is registered. Refs make the intent explicit and
    // let a pending timeout be cancelled on unmount.
    const batteryReadingTimerId = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)
    const pressureReadingTimerId = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

    useEffect(
        () => () => {
            clearTimeout(batteryReadingTimerId.current)
            clearTimeout(pressureReadingTimerId.current)
        },
        []
    )

    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(SignalREventLabels.robotTelemetryUpdated, (username: string, message: string) => {
            const robotPropertyUpdate: RobotTelemetryPropertyUpdate = JSON.parse(message)
            if (robotPropertyUpdate.robotId === robotId) {
                if (robotPropertyUpdate.telemetryName === 'batteryLevel') {
                    setRobotBatteryLevel(robotPropertyUpdate.telemetryValue as number)
                    clearTimeout(batteryReadingTimerId.current)
                    // Time in milliseconds
                    batteryReadingTimerId.current = setTimeout(() => setRobotBatteryLevel(undefined), 30 * 1000)
                } else if (robotPropertyUpdate.telemetryName === 'pressureLevel') {
                    setRobotPressureLevel(robotPropertyUpdate.telemetryValue as number)
                    clearTimeout(pressureReadingTimerId.current)
                    // Time in milliseconds
                    pressureReadingTimerId.current = setTimeout(() => setRobotPressureLevel(undefined), 30 * 1000)
                } else if (robotPropertyUpdate.telemetryName === 'batteryState') {
                    setRobotBatteryStatus(robotPropertyUpdate.telemetryValue as BatteryStatus)
                } else if (robotPropertyUpdate.telemetryName === 'pose') {
                    setRobotPose(robotPropertyUpdate.telemetryValue as Pose)
                }
            }
        })
        // Keyed on the robot id rather than the robot object: the object identity changes
        // on every fetch, which would re-subscribe on every telemetry update.
    }, [registerEvent, connectionReady, robotId])

    return { robotBatteryLevel, robotBatteryStatus, robotPressureLevel, robotPose }
}

interface RobotIdAndPose {
    robotId: string
    pose: Pose
}

export const useAllRobotPosesTelemetry = () => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const { enabledRobots } = useAssetContext()

    const [robotIdAndPoses, setRobotIdAndPoses] = useState<RobotIdAndPose[]>([])

    useEffect(() => {
        if (!connectionReady) return
        return registerEvent(SignalREventLabels.robotTelemetryUpdated, (username: string, message: string) => {
            const robotPropertyUpdate: RobotTelemetryPropertyUpdate = JSON.parse(message)
            if (
                robotPropertyUpdate.telemetryName === 'pose' &&
                enabledRobots.map((r) => r.id).includes(robotPropertyUpdate.robotId)
            ) {
                setRobotIdAndPoses((prev) => [
                    ...prev.filter((r) => r.robotId !== robotPropertyUpdate.robotId),
                    { robotId: robotPropertyUpdate.robotId, pose: robotPropertyUpdate.telemetryValue as Pose },
                ])
            }
        })
    }, [registerEvent, connectionReady, enabledRobots])

    return { robotIdAndPoses }
}
