import { RefObject, useEffect, useEffectEvent } from 'react'

export const useVideoAspectRatio = (
    container: RefObject<HTMLElement | null>,
    rotationDegrees: number,
    onAspectRatioChange: (aspectRatio: number) => void
) => {
    const reportAspectRatio = useEffectEvent(onAspectRatioChange)

    useEffect(() => {
        const element = container.current
        if (!element) return

        const updateAspectRatio = () => {
            const video = element.querySelector('video')
            if (!video || video.videoWidth <= 0 || video.videoHeight <= 0) return

            const quarterTurn = Math.abs(rotationDegrees % 180) === 90
            const aspectRatio = video.videoWidth / video.videoHeight
            reportAspectRatio(quarterTurn ? 1 / aspectRatio : aspectRatio)
        }

        updateAspectRatio()
        element.addEventListener('loadedmetadata', updateAspectRatio, true)
        element.addEventListener('resize', updateAspectRatio, true)
        return () => {
            element.removeEventListener('loadedmetadata', updateAspectRatio, true)
            element.removeEventListener('resize', updateAspectRatio, true)
        }
    }, [container, rotationDegrees])
}
