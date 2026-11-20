export const CAMERA_MIN_WIDTH_REM = 12
export const CAMERA_MEDIA_HEIGHT_REM = 15
export const CAMERA_LABEL_HEIGHT_REM = 1.5
export const CAMERA_GAP_REM = 1

export interface CameraLayoutItem {
    key: string
    aspectRatio: number
}

export interface CameraPosition {
    left: number
    top: number
    width: number
}

export const getCameraPositions = (cameras: CameraLayoutItem[], containerWidth: number, remSize: number) => {
    const positions: Record<string, CameraPosition> = {}
    if (containerWidth <= 0) return positions

    const gap = CAMERA_GAP_REM * remSize
    const minimumWidth = CAMERA_MIN_WIDTH_REM * remSize
    const maximumMediaHeight = CAMERA_MEDIA_HEIGHT_REM * remSize
    const reservedRowHeight = (CAMERA_LABEL_HEIGHT_REM + CAMERA_MEDIA_HEIGHT_REM) * remSize
    const columns = Math.max(1, Math.floor((containerWidth + gap) / (minimumWidth + gap)))

    for (let start = 0; start < cameras.length; start += columns) {
        const row = cameras.slice(start, start + columns)
        const combinedAspectRatio = row.reduce((total, camera) => total + camera.aspectRatio, 0)
        const availableWidth = containerWidth - (row.length - 1) * gap
        const mediaHeight = Math.min(maximumMediaHeight, availableWidth / combinedAspectRatio)
        const top = gap + (start / columns) * (reservedRowHeight + gap)
        let left = 0

        for (const camera of row) {
            const width = camera.aspectRatio * mediaHeight
            positions[camera.key] = { left, top, width }
            left += width + gap
        }
    }

    return positions
}
