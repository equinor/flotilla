import { RefObject, useEffect, useState } from 'react'

export const useVideoAspectRatio = (
    container: RefObject<HTMLElement | null>,
    rotationDegrees = 0,
    onAspectRatioChange?: (aspectRatio: number) => void
) => {
    const [sourceAspectRatio, setSourceAspectRatio] = useState(16 / 9)
    const quarterTurn = Math.abs(rotationDegrees % 180) === 90
    const aspectRatio = quarterTurn ? 1 / sourceAspectRatio : sourceAspectRatio

    useEffect(() => {
        const element = container.current
        if (!element) return

        const updateAspectRatio = () => {
            const video = element.querySelector('video')
            if (video && video.videoWidth > 0 && video.videoHeight > 0) {
                setSourceAspectRatio(video.videoWidth / video.videoHeight)
            }
        }

        updateAspectRatio()
        element.addEventListener('loadedmetadata', updateAspectRatio, true)
        element.addEventListener('resize', updateAspectRatio, true)
        return () => {
            element.removeEventListener('loadedmetadata', updateAspectRatio, true)
            element.removeEventListener('resize', updateAspectRatio, true)
        }
    }, [container])

    useEffect(() => {
        onAspectRatioChange?.(aspectRatio)
    }, [aspectRatio, onAspectRatioChange])

    return aspectRatio
}
