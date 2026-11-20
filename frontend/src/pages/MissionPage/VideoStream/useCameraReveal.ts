import { useEffect, useState } from 'react'

const CAMERA_REVEAL_TIMEOUT_MS = 5000

export const useCameraReveal = (robotId: string, cameraKeys: string[], aspectRatios: Record<string, number>) => {
    const [revealedRobotId, setRevealedRobotId] = useState<string>()
    const hasCameras = cameraKeys.length > 0
    const allCamerasReady = hasCameras && cameraKeys.every((key) => aspectRatios[key] !== undefined)
    const isRevealed = revealedRobotId === robotId || allCamerasReady

    if (allCamerasReady && revealedRobotId !== robotId) setRevealedRobotId(robotId)

    useEffect(() => {
        if (!hasCameras || isRevealed) return

        const timeout = setTimeout(() => setRevealedRobotId(robotId), CAMERA_REVEAL_TIMEOUT_MS)
        return () => clearTimeout(timeout)
    }, [robotId, hasCameras, isRevealed])

    return isRevealed
}
