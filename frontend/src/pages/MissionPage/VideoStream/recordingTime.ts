export const getRecordingTimestamp = (value: Date | string | undefined): number | undefined => {
    if (!value) return undefined
    const timestamp = new Date(value).getTime()
    return Number.isFinite(timestamp) ? timestamp : undefined
}

export const formatRecordingTime = (seconds: number): string => {
    const wholeSeconds = Math.floor(seconds)
    const minutes = Math.floor(wholeSeconds / 60)
    return `${minutes}:${String(wholeSeconds % 60).padStart(2, '0')}`
}
