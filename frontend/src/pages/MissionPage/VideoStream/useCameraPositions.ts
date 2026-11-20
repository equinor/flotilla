import { RefObject, useEffect, useState } from 'react'
import { CameraLayoutItem, getCameraPositions } from './cameraPositions'

export const useCameraPositions = (container: RefObject<HTMLDivElement | null>, cameras: CameraLayoutItem[]) => {
    const [size, setSize] = useState({ width: 0, remSize: 16 })

    useEffect(() => {
        const element = container.current
        if (!element) return

        const updateSize = () => {
            const width = element.getBoundingClientRect().width
            const remSize = parseFloat(getComputedStyle(document.documentElement).fontSize) || 16
            setSize((previous) =>
                previous.width === width && previous.remSize === remSize ? previous : { width, remSize }
            )
        }

        if (typeof ResizeObserver === 'undefined') {
            const timeout = setTimeout(updateSize, 0)
            window.addEventListener('resize', updateSize)
            return () => {
                clearTimeout(timeout)
                window.removeEventListener('resize', updateSize)
            }
        }

        const observer = new ResizeObserver(updateSize)
        observer.observe(element)
        return () => observer.disconnect()
    }, [container])

    return getCameraPositions(cameras, size.width, size.remSize)
}
