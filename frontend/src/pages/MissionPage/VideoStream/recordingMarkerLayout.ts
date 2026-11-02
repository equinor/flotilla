interface MarkerBounds {
    left: number
    width: number
}

export const getRecordingMarkerRows = (markers: MarkerBounds[]): number[] => {
    const rowEnds: number[] = []
    const rows = new Array<number>(markers.length)
    const sortedMarkers = markers
        .map((marker, index) => ({ ...marker, index }))
        .sort((first, second) => first.left - second.left)

    for (const { left, width, index } of sortedMarkers) {
        const start = left - width / 2
        const availableRow = rowEnds.findIndex((end) => start >= end + 4)
        const row = availableRow === -1 ? rowEnds.length : availableRow
        rowEnds[row] = left + width / 2
        rows[index] = row
    }

    return rows
}
