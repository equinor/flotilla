import { useState, useEffect } from 'react'
import { Robot, RobotPropertyUpdate } from 'models/Robot'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { BackendAPICaller } from 'api/ApiCaller'

export const useRobot = (robotId: string) => {
    const [isFetchingRobot, setIsFetchingRobot] = useState<boolean>(true)
    const [robot, setRobot] = useState<Robot | undefined>(undefined)
    const { registerEvent, connectionReady } = useSignalRContext()

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

    useEffect(() => {
        if (connectionReady && !isFetchingRobot) {
            if (robot === undefined) {
                console.error(`Not fetching robot, but robot is not fetched ${robotId}:`)
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
