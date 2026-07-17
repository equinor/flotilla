import { useEffect, useRef } from 'react'

export const useOnPageVisible = (onVisible: () => void) => {
    const savedCallback = useRef(onVisible)

    useEffect(() => {
        savedCallback.current = onVisible
    }, [onVisible])

    useEffect(() => {
        const handleVisibilityChange = () => {
            if (document.visibilityState === 'visible') savedCallback.current()
        }
        document.addEventListener('visibilitychange', handleVisibilityChange)
        return () => document.removeEventListener('visibilitychange', handleVisibilityChange)
    }, [])
}
