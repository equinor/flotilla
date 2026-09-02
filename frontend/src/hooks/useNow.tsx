import { useEffect, useState } from 'react'
import { useOnPageVisible } from './usePageVisibility'

const TICK_MS = 10 * 1000

// Ticks every 10s (and on tab refocus) so day-of-week/time-based UI stays correct
export const useNow = () => {
    const [now, setNow] = useState(() => new Date())

    useEffect(() => {
        const interval = setInterval(() => setNow(new Date()), TICK_MS)
        return () => clearInterval(interval)
    }, [])

    useOnPageVisible(() => setNow(new Date()))
    return now
}
